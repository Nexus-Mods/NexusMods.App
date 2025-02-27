using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Security.Cryptography;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Types;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Games.FOMOD;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NexusMods.Collections;
using CollectionMod = Mod;

[PublicAPI]
public class InstallCollectionDownloadJob : IJobDefinitionWithStart<InstallCollectionDownloadJob, LoadoutItemGroup.ReadOnly>
{
    public required CollectionDownload.ReadOnly Item { get; init; }
    public required CollectionGroup.ReadOnly Group { get; init; }
    public required LoadoutId TargetLoadout { get; init; }
    public required CollectionMod CollectionMod { get; init; }
    public required NexusModsCollectionLibraryFile.ReadOnly SourceCollection { get; init; }
    private LibraryArchive.ReadOnly SourceCollectionArchive => SourceCollection.AsLibraryFile().ToLibraryArchive();

    public required IServiceProvider ServiceProvider { get; init; }
    public required IConnection Connection { get; init; }
    public required IFileStore FileStore { get; init; }
    public required ILibraryService LibraryService { get; init; }

    public ILibraryItemInstaller? FallbackInstaller { get; init; }
    public Optional<GamePath> FallbackCollectionInstallDirectory { get; init; }

    public static async ValueTask<InstallCollectionDownloadJob> Create(
        IServiceProvider serviceProvider,
        LoadoutId targetLoadout,
        CollectionDownload.ReadOnly download,
        CancellationToken cancellationToken)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var optionalCollectionGroup = CollectionDownloader.GetCollectionGroup(download.CollectionRevision, targetLoadout, connection.Db);
        if (!optionalCollectionGroup.HasValue) throw new InvalidOperationException("Collection must exist!");
        var collectionGroup = optionalCollectionGroup.Value.AsCollectionGroup();

        var sourceCollection = serviceProvider.GetRequiredService<CollectionDownloader>().GetLibraryFile(download.CollectionRevision);
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();

        var root = await nexusModsLibrary.ParseCollectionJsonFile(sourceCollection, cancellationToken);
        var collectionMod = root.Mods[download.ArrayIndex];

        return new InstallCollectionDownloadJob
        {
            Item = download,
            CollectionMod = collectionMod,
            Group = collectionGroup,
            TargetLoadout = targetLoadout,
            SourceCollection = sourceCollection,

            ServiceProvider = serviceProvider,
            Connection = connection,
            FileStore = serviceProvider.GetRequiredService<IFileStore>(),
            LibraryService = serviceProvider.GetRequiredService<ILibraryService>(),
        };
    }

    /// <inheritdoc/>
    public async ValueTask<LoadoutItemGroup.ReadOnly> StartAsync(IJobContext<InstallCollectionDownloadJob> context)
    {
        var group = await Install(context);

        // Add missing data from the collection to the item
        using var tx = Connection.BeginTransaction();
        tx.Add(group.Id, LoadoutItem.Name, CollectionMod.Source.LogicalFilename ?? CollectionMod.Name);
        tx.Add(group.Id, NexusCollectionItemLoadoutGroup.Download, Item);
        tx.Add(group.Id, NexusCollectionItemLoadoutGroup.IsRequired, Item.IsRequired);
        var result = await tx.Commit();

        return new LoadoutItemGroup.ReadOnly(result.Db, group.Id);
    }

    private async ValueTask<LoadoutItemGroup.ReadOnly> Install(IJobContext<InstallCollectionDownloadJob> context)
    {
        if (Item.TryGetAsCollectionDownloadBundled(out var bundledDownload))
        {
            return await InstallBundledMod(bundledDownload);
        }

        if (CollectionMod.Hashes.Length > 0)
        {
            return await InstallReplicatedMod();
        }

        if (CollectionMod.Choices is { Type: ChoicesType.fomod })
        {
            return await InstallFomodWithPredefinedChoices(context.CancellationToken);
        }

        var libraryFile = GetLibraryFile(Item, Connection.Db);
        return await LibraryService.InstallItem(
            libraryFile.AsLibraryItem(),
            TargetLoadout,
            parent: Group.AsLoadoutItemGroup().LoadoutItemGroupId,
            // NOTE(erri120): https://github.com/Nexus-Mods/NexusMods.App/issues/2553
            // The advanced installer shouldn't appear when installing collections,
            // the decision was made that the app should behave similar to Vortex,
            // which installs unknown stuff into a "default folder"
            fallbackInstaller: FallbackInstaller
        );
    }

    private async Task<LoadoutItemGroup.ReadOnly> InstallBundledMod(CollectionDownloadBundled.ReadOnly download)
    {
        // Bundled mods are found inside the collection archive, so we'll have to find the files that are prefixed with the mod's source file expression.
        var prefixPath = "bundled".ToRelativePath().Join(download.BundledPath);
        var prefixFiles = SourceCollectionArchive.Children.Where(f => f.Path.InFolder(prefixPath)).ToArray();

        using var tx = Connection.BeginTransaction();

        var modGroup = new NexusCollectionBundledLoadoutGroup.New(tx, out var id)
        {
            CollectionLibraryFileId = SourceCollection,
            BundleDownloadId = download,
            NexusCollectionItemLoadoutGroup = new NexusCollectionItemLoadoutGroup.New(tx, id)
            {
                IsRequired = download.AsCollectionDownload().IsRequired,
                DownloadId = download.AsCollectionDownload(),
                LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
                {
                    IsGroup = true,
                    LoadoutItem = new LoadoutItem.New(tx, id)
                    {
                        Name = download.AsCollectionDownload().Name,
                        LoadoutId = TargetLoadout,
                        ParentId = Group.Id,
                    },
                },
            },
        };

        // NOTE(erri120): for details see https://github.com/Nexus-Mods/NexusMods.App/issues/2630#issuecomment-2653787872
        var parentPath = FallbackCollectionInstallDirectory.ValueOr(() => new GamePath(LocationId.Game, ""));

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
                    TargetPath = (fileId, parentPath.LocationId, parentPath.Path.Join(fixedPath)),
                    LoadoutItem = new LoadoutItem.New(tx, fileId)
                    {
                        Name = file.Path,
                        LoadoutId = TargetLoadout,
                        ParentId = modGroup.Id,
                    },
                },
            };
        }

        var result = await tx.Commit();
        return result.Remap(modGroup).AsNexusCollectionItemLoadoutGroup().AsLoadoutItemGroup();
    }

    /// <summary>
    /// Install a fomod with predefined choices.
    /// </summary>
    private async Task<LoadoutItemGroup.ReadOnly> InstallFomodWithPredefinedChoices(CancellationToken cancellationToken)
    {
        var libraryFile = GetLibraryFile(Item, Connection.Db);
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive))
            throw new NotImplementedException();

        var fomodInstaller = FomodXmlInstaller.Create(ServiceProvider, new GamePath(LocationId.Game, ""));

        using var tx = Connection.BeginTransaction();
        var group = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = Item.Name,
                LoadoutId = TargetLoadout,
                ParentId = Group.Id,
            },
        };

        _ = new LibraryLinkedLoadoutItem.New(tx, id)
        {
            LibraryItemId = libraryFile.AsLibraryItem(),
            LoadoutItemGroup = group,
        };

        var loadout = new Loadout.ReadOnly(Connection.Db, TargetLoadout);

        var options = CollectionMod.Choices!.Options;
        await fomodInstaller.ExecuteAsync(libraryArchive, group, tx, loadout, options, cancellationToken: cancellationToken);

        var result = await tx.Commit();
        return result.Remap(group);
    }

    /// <summary>
    /// This sort of install is a bit strange. The Hashes field contains pairs of MD5 hashes and paths. The paths are
    /// the target locations of the mod files. The MD5 hashes are the hashes of the files. So it's a fromHash->toPath
    /// situation. We don't store the MD5 hashes in the database, so we'll have to calculate them on the fly.
    /// </summary>
    private async Task<LoadoutItemGroup.ReadOnly> InstallReplicatedMod()
    {
        // So collections hash everything by MD5, so we'll have to collect MD5 information for the files in the archive.
        // We don't do this during indexing into the library because this is the only case where we need MD5 hashes.
        ConcurrentDictionary<Md5HashValue, HashMapping> hashes = new();

        var libraryFile = GetLibraryFile(Item, Connection.Db);
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
        if (CollectionMod.Patches.Count > 0) await PatchFiles(libraryArchive, hashes);

        using var tx = Connection.BeginTransaction();

        var group = new NexusCollectionReplicatedLoadoutGroup.New(tx, out var id)
        {
            IsReplicated = true,
            NexusCollectionItemLoadoutGroup = new NexusCollectionItemLoadoutGroup.New(tx, id)
            {
                DownloadId = Item,
                IsRequired = Item.IsRequired,
                LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
                {
                    IsGroup = true,
                    LoadoutItem = new LoadoutItem.New(tx, id)
                    {
                        Name = Item.Name,
                        LoadoutId = TargetLoadout,
                        ParentId = Group.Id,
                    },
                },
            },
        };

        _ = new LibraryLinkedLoadoutItem.New(tx, id)
        {
            LibraryItemId = libraryFile.AsLibraryItem(),
            LoadoutItemGroup = group.GetNexusCollectionItemLoadoutGroup(tx).GetLoadoutItemGroup(tx),
        };

        // Now we map the files to their locations based on the hashes
        foreach (var pair in CollectionMod.Hashes)
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
                        LoadoutId = TargetLoadout,
                        ParentId = group.Id,
                    },
                },
            };
        }

        var result = await tx.Commit();
        return new LoadoutItemGroup.ReadOnly(result.Db, result[group.Id]);
    }

    /// <summary>
    /// This will go through and generate all the patch files for the given archive based on the mod's patches.
    /// </summary>
    private async Task PatchFiles(
        LibraryArchive.ReadOnly modArchive,
        ConcurrentDictionary<Md5HashValue, HashMapping> hashes)
    {
        // Index all the files in the collection zip file and the mod archive by their paths so we can find them easily.
        var modChildren = IndexChildren(modArchive);
        var collectionChildren = IndexChildren(SourceCollectionArchive);

        // These are the generated patch files that we'll need to add to the file store.
        ConcurrentBag<ArchivedFileEntry> patchedFiles = [];

        await Parallel.ForEachAsync(CollectionMod.Patches, async (patch, token) =>
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
            var patchName = RelativePath.FromUnsanitizedInput("patches/" + CollectionMod.Name + "/" + pathString + ".diff");
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
        });

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

    private LibraryFile.ReadOnly GetLibraryFile(CollectionDownload.ReadOnly download, IDb db)
    {
        var status = CollectionDownloader.GetStatus(download, Group, db);
        if (!status.IsInLibrary(out var libraryItem)) throw new NotImplementedException();
        if (!libraryItem.TryGetAsLibraryFile(out var libraryFile)) throw new NotImplementedException();
        return libraryFile;
    }
}
