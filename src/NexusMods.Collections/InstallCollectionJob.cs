using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Collections.Types;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Games.FOMOD;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Collections;

using ModAndDownload = (Mod Mod, CollectionDownload.ReadOnly Download);

/// <summary>
/// Job for installing a collection.
/// </summary>
public class InstallCollectionJob : IJobDefinitionWithStart<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>
{ 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public required NexusModsCollectionLibraryFile.ReadOnly SourceCollection { get; init; }
    private LibraryArchive.ReadOnly SourceCollectionArchive => SourceCollection.AsLibraryFile().ToLibraryArchive();
    public required CollectionRevisionMetadata.ReadOnly RevisionMetadata { get; init; }
    public required IFileStore FileStore { get; init; }
    public required ILibraryService LibraryService { get; init; }
    public required IConnection Connection { get; init; }
    public required LoadoutId TargetLoadout { get; init; }
    public required IServiceProvider ServiceProvider { get; init; }
    public required NexusModsLibrary NexusModsLibrary { get; init; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Factory.
    /// </summary>
    public static IJobTask<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly> Create(
        IServiceProvider provider,
        LoadoutId target,
        NexusModsCollectionLibraryFile.ReadOnly source,
        CollectionRevisionMetadata.ReadOnly revisionMetadata)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new InstallCollectionJob
        {
            TargetLoadout = target,
            SourceCollection = source,
            RevisionMetadata = revisionMetadata,
            ServiceProvider = provider,
            FileStore = provider.GetRequiredService<IFileStore>(),
            LibraryService = provider.GetRequiredService<ILibraryService>(),
            Connection = provider.GetRequiredService<IConnection>(),
            NexusModsLibrary = provider.GetRequiredService<NexusModsLibrary>(),
        };

        return monitor.Begin<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>(job);
    }

    /// <summary>
    /// Factory.
    /// </summary>
    public static IJobTask<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly> Create(
        IServiceProvider provider,
        LoadoutId target,
        CollectionRevisionMetadata.ReadOnly revisionMetadata)
    {
        var connection = provider.GetRequiredService<IConnection>();
        var datoms = connection.Db.Datoms(
            (NexusModsCollectionLibraryFile.CollectionSlug, revisionMetadata.Collection.Slug),
            (NexusModsCollectionLibraryFile.CollectionRevisionNumber, revisionMetadata.RevisionNumber)
        );

        if (datoms.Count == 0) throw new Exception($"Unable to find collection file for revision `{revisionMetadata.Collection.Slug}` (`{revisionMetadata.RevisionNumber}`)");
        var source = NexusModsCollectionLibraryFile.Load(connection.Db, datoms[0]);

        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new InstallCollectionJob
        {
            TargetLoadout = target,
            SourceCollection = source,
            RevisionMetadata = revisionMetadata,
            ServiceProvider = provider,
            FileStore = provider.GetRequiredService<IFileStore>(),
            Connection = connection,
            LibraryService = provider.GetRequiredService<ILibraryService>(),
            NexusModsLibrary = provider.GetRequiredService<NexusModsLibrary>(),
        };

        return monitor.Begin<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>(job);
    }

    /// <summary>
    /// Installs the collection.
    /// </summary>
    public async ValueTask<NexusCollectionLoadoutGroup.ReadOnly> StartAsync(IJobContext<InstallCollectionJob> context)
    {
        var isReady = CollectionDownloader.IsFullyDownloaded(RevisionMetadata, onlyRequired: true, db: Connection.Db);
        if (!isReady) throw new InvalidOperationException("The collection hasn't fully been downloaded!");

        var root = await NexusModsLibrary.ParseCollectionJsonFile(SourceCollection, context.CancellationToken);
        var modsAndDownloads = GatherDownloads(root);

        NexusCollectionLoadoutGroup.ReadOnly collectionGroup;
        using (var tx = Connection.BeginTransaction())
        {
            var group = new NexusCollectionLoadoutGroup.New(tx, out var id)
            {
                CollectionId = RevisionMetadata.Collection,
                RevisionId = RevisionMetadata,
                LibraryFileId = SourceCollection,
                CollectionGroup = new CollectionGroup.New(tx, id)
                {
                    IsReadOnly = true,
                    LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
                    {
                        IsGroup = true,
                        LoadoutItem = new LoadoutItem.New(tx, id)
                        {
                            Name = root.Info.Name,
                            LoadoutId = TargetLoadout,
                        },
                    },
                },
            };

            var groupResult = await tx.Commit();
            collectionGroup = groupResult.Remap(group);
        }

        var installed = new ConcurrentBag<(ModAndDownload, LoadoutItemGroup.ReadOnly)>();
        await Parallel.ForEachAsync(modsAndDownloads, context.CancellationToken, async (modAndDownload, _) =>
        {
            var result = await InstallMod(modAndDownload, TargetLoadout, collectionGroup.AsCollectionGroup().AsLoadoutItemGroup());
            installed.Add((modAndDownload, result));
        });

        using (var tx = Connection.BeginTransaction())
        {
            foreach (var (tuple, modGroup) in installed)
            {
                tx.Add(modGroup.Id, LoadoutItem.Name, tuple.Mod.Name);
            }

            await tx.Commit();
        }

        return collectionGroup;
    }

    private List<ModAndDownload> GatherDownloads(CollectionRoot root)
    {
        var map = RevisionMetadata.Downloads.ToDictionary(static download => download.ArrayIndex, static download => download);
        var list = new List<ModAndDownload>();

        for (var i = 0; i < root.Mods.Length; i++)
        {
            var mod = root.Mods[i];

            // TODO: figure out what to do with optional mods
            if (mod.Optional) continue;

            if (!map.TryGetValue(i, out var download)) throw new NotImplementedException();
            list.Add((mod, download));
        }

        return list;
    }

    private async Task<LoadoutItemGroup.ReadOnly> InstallMod(
        ModAndDownload modAndDownload,
        LoadoutId loadoutId,
        LoadoutItemGroup.ReadOnly group)
    {
        var (mod, download) = modAndDownload;
        if (download.TryGetAsCollectionDownloadBundled(out var bundledDownload))
        {
            return await InstallBundledMod(loadoutId, bundledDownload, group);
        }

        if (mod.Hashes.Length > 0)
        {
            return await InstallReplicatedMod(loadoutId, modAndDownload, group);
        }

        if (mod.Choices is { Type: ChoicesType.fomod })
        {
            return await InstallFomodWithPredefinedChoices(loadoutId, modAndDownload, group);
        }

        var libraryFile = GetLibraryFile(download, Connection.Db);
        return await LibraryService.InstallItem(libraryFile.AsLibraryItem(), loadoutId, parent: group.LoadoutItemGroupId);
    }

    private async Task<LoadoutItemGroup.ReadOnly> InstallBundledMod(
        LoadoutId loadoutId,
        CollectionDownloadBundled.ReadOnly download,
        LoadoutItemGroup.ReadOnly group)
    {
        // Bundled mods are found inside the collection archive, so we'll have to find the files that are prefixed with the mod's source file expression.
        var prefixPath = "bundled".ToRelativePath().Join(download.BundledPath);
        var prefixFiles = SourceCollectionArchive.Children.Where(f => f.Path.InFolder(prefixPath)).ToArray();

        using var tx = Connection.BeginTransaction();

        var modGroup = new NexusCollectionBundledLoadoutGroup.New(tx, out var id)
        {
            CollectionLibraryFileId = SourceCollection,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, id)
                {
                    Name = download.AsCollectionDownload().Name,
                    LoadoutId = loadoutId,
                    ParentId = group.Id,
                },
            },
        };

        foreach (var file in prefixFiles)
        {
            // Remove the prefix path from the file path
            var fixedPath = file.Path.RelativeTo(prefixPath);
            
            // Fill out the rest of the file information
            _ = new LoadoutFile.New(tx, out var fileId)
            {
                Hash = file.AsLibraryFile().Hash,
                Size = file.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, fileId)
                {
                    TargetPath = (fileId, LocationId.Game, fixedPath),
                    LoadoutItem = new LoadoutItem.New(tx, fileId)
                    {
                        Name = file.Path,
                        LoadoutId = loadoutId,
                        ParentId = modGroup.Id,
                    },
                },
            };
        }

        var result = await tx.Commit();
        return result.Remap(modGroup).AsLoadoutItemGroup();
    }

    /// <summary>
    /// Install a fomod with predefined choices.
    /// </summary>
    private async Task<LoadoutItemGroup.ReadOnly> InstallFomodWithPredefinedChoices(
        LoadoutId loadoutId,
        ModAndDownload modAndDownload,
        LoadoutItemGroup.ReadOnly collectionGroup)
    {
        var (mod, download) = modAndDownload;

        var libraryFile = GetLibraryFile(download, Connection.Db);
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive))
            throw new NotImplementedException();

        var fomodInstaller = FomodXmlInstaller.Create(ServiceProvider, new GamePath(LocationId.Game, ""));

        using var tx = Connection.BeginTransaction();
        var group = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = download.Name,
                LoadoutId = loadoutId,
                ParentId = collectionGroup.Id,
            },
        };

        var loadout = new Loadout.ReadOnly(Connection.Db, loadoutId);

        var options = mod.Choices!.Options;
        await fomodInstaller.ExecuteAsync(libraryArchive, group, tx, loadout, options, CancellationToken.None);

        var result = await tx.Commit();
        return result.Remap(group);
    }

    /// <summary>
    /// This sort of install is a bit strange. The Hashes field contains pairs of MD5 hashes and paths. The paths are
    /// the target locations of the mod files. The MD5 hashes are the hashes of the files. So it's a fromHash->toPath
    /// situation. We don't store the MD5 hashes in the database, so we'll have to calculate them on the fly.
    /// </summary>
    private async Task<LoadoutItemGroup.ReadOnly> InstallReplicatedMod(
        LoadoutId loadoutId,
        ModAndDownload modAndDownload,
        LoadoutItemGroup.ReadOnly parentGroup)
    {
        var (mod, download) = modAndDownload;

        // So collections hash everything by MD5, so we'll have to collect MD5 information for the files in the archive.
        // We don't do this during indexing into the library because this is the only case where we need MD5 hashes.
        ConcurrentDictionary<Md5HashValue, HashMapping> hashes = new();

        var libraryFile = GetLibraryFile(download, Connection.Db);
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive))
            throw new NotImplementedException();

        await Parallel.ForEachAsync(libraryArchive.Children, async (child, token) =>
        {
            await using var stream = await FileStore.GetFileStream(child.AsLibraryFile().Hash, token);
            using var hasher = MD5.Create();
            var hash = await hasher.ComputeHashAsync(stream, token);
            var md5 = Md5HashValue.From(hash);

            var file = child.AsLibraryFile();
            hashes[md5] = new HashMapping()
            {
                Hash = file.Hash,
                Size = file.Size,
            };
        });

        // If we have any binary patching to do, then we'll do that now.
        if (mod.Patches.Count > 0) await PatchFiles(mod, libraryArchive, SourceCollectionArchive, hashes);

        using var tx = Connection.BeginTransaction();
        
        // Create the group
        var group = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = download.Name,
                LoadoutId = loadoutId,
                ParentId = parentGroup.Id,
            },
        };
        
        // Link the group to the loadout
        _ = new LibraryLinkedLoadoutItem.New(tx, id)
        {
            LibraryItemId = libraryFile.AsLibraryItem(),
            LoadoutItemGroup = group,
        };

        // Now we map the files to their locations based on the hashes
        foreach (var pair in mod.Hashes)
        {
            // Try and find the hash we are looking for
            if (!hashes.TryGetValue(pair.MD5, out var libraryItem))
                throw new InvalidOperationException("The hash was not found in the archive.");

            // Map the file to the specific path
            _ = new LoadoutFile.New(tx, out var fileId)
            {
                Hash = libraryItem.Hash,
                Size = libraryItem.Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, fileId)
                {
                    TargetPath = (fileId, LocationId.Game, pair.Path),
                    LoadoutItem = new LoadoutItem.New(tx, fileId)
                    {
                        Name = pair.Path,
                        LoadoutId = loadoutId,
                        ParentId = group.Id,
                    },
                },
            };
        }

        var result = await tx.Commit();
        return result.Remap(group);
    }

    /// <summary>
    /// This will go through and generate all the patch files for the given archive based on the mod's patches.
    /// </summary>
    private async Task PatchFiles(
        Mod modInfo,
        LibraryArchive.ReadOnly modArchive,
        LibraryArchive.ReadOnly collectionArchive,
        ConcurrentDictionary<Md5HashValue, HashMapping> hashes)
    {
        // Index all the files in the collection zip file and the mod archive by their paths so we can find them easily.
        var modChildren = IndexChildren(modArchive);
        var collectionChildren = IndexChildren(collectionArchive);
        
        // These are the generated patch files that we'll need to add to the file store.
        ConcurrentBag<ArchivedFileEntry> patchedFiles = [];
        
        await Parallel.ForEachAsync(modInfo.Patches, async (patch, token) =>
            {
                var (pathString, srcCrc) = patch;
                var srcPath = RelativePath.FromUnsanitizedInput(pathString);
                
                if (!modChildren.TryGetValue(srcPath, out var file))
                    throw new InvalidOperationException("The file to patch was not found in the archive.");

                // Load the source file and check the CRC32 hash
                var srcData = (await FileStore.Load(file.Hash, token)).ToArray();
                
                // Calculate the CRC32 hash of the source file
                var srcCrc32 = Crc32.HashToUInt32(srcData.AsSpan());
                if (srcCrc32 != srcCrc)
                    throw new InvalidOperationException("The source file's CRC32 hash does not match the expected hash.");
                
                // Load the patch file
                var patchName = RelativePath.FromUnsanitizedInput("patches/" + modInfo.Name + "/" + pathString + ".diff");
                if (!collectionChildren.TryGetValue(patchName, out var patchFile))
                    throw new InvalidOperationException("The patch file was not found in the archive.");
                
                var patchedFile = new MemoryStream();
                var patchData = (await FileStore.Load(patchFile.Hash, token)).ToArray();
                
                // Generate the patched file
                BsDiff.BinaryPatch.Apply(new MemoryStream(srcData), () => new MemoryStream(patchData), patchedFile);
                
                var patchedArray = patchedFile.ToArray();
                
                // Hash the patched file and add it to the patched files list
                using var md5 = MD5.Create();
                md5.ComputeHash(patchedArray);
                var md5Hash = Md5HashValue.From(md5.Hash!);
                var xxHash = patchedArray.xxHash3();
                
                patchedFiles.Add(new ArchivedFileEntry(new MemoryStreamFactory(srcPath, patchedFile), xxHash, Size.FromLong(patchedFile.Length)));
                hashes[md5Hash] = new HashMapping
                {
                    Hash = xxHash,
                    Size = Size.FromLong(patchedFile.Length),
                };
            }
        );
        
        // Backup the patched files
        await FileStore.BackupFiles(patchedFiles, deduplicate: true);
    }

    private static Dictionary<RelativePath, LibraryFile.ReadOnly> IndexChildren(LibraryArchive.ReadOnly archive)
    {
        var children = archive.Children
            .Select(static child => (child.Path, child.AsLibraryFile()))
            .ToDictionary(static kv => kv.Item1, static kv => kv.Item2);

        return children;
    }

    private static LibraryFile.ReadOnly GetLibraryFile(CollectionDownload.ReadOnly download, IDb db)
    {
        if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
        {
            if (!CollectionDownloader.TryGetDownloadedItem(nexusModsDownload, db, out var item))
                throw new NotImplementedException();

            var libraryFile = LibraryFile.Load(item.Db, item.Id);
            if (!libraryFile.IsValid()) throw new NotImplementedException();
            return libraryFile;
        }

        if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
        {
            if (!CollectionDownloader.TryGetDownloadedItem(externalDownload, db, out var item))
                throw new NotImplementedException();

            return item;
        }

        throw new NotImplementedException();
    }
}
