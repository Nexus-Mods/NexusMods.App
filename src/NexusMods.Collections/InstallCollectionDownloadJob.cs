using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Security.Cryptography;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Types;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Games.FOMOD;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.IO;
using NexusMods.Sdk.Jobs;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NexusMods.Collections;
using CollectionMod = Mod;

[PublicAPI]
public class InstallCollectionDownloadJob : IJobDefinitionWithStart<InstallCollectionDownloadJob, LoadoutItemGroup.ReadOnly>
{
    public required ILogger Logger { get; init; }
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
    public required ILoadoutManager LoadoutManager { get; init; }

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
            Logger = serviceProvider.GetRequiredService<ILogger<InstallCollectionDownloadJob>>(),
            Item = download,
            CollectionMod = collectionMod,
            Group = collectionGroup,
            TargetLoadout = targetLoadout,
            SourceCollection = sourceCollection,

            ServiceProvider = serviceProvider,
            Connection = connection,
            FileStore = serviceProvider.GetRequiredService<IFileStore>(),
            LibraryService = serviceProvider.GetRequiredService<ILibraryService>(),
            LoadoutManager = serviceProvider.GetRequiredService<ILoadoutManager>(),
        };
    }

    /// <inheritdoc/>
    public async ValueTask<LoadoutItemGroup.ReadOnly> StartAsync(IJobContext<InstallCollectionDownloadJob> context)
    {
        var (group, patchedFiles) = await Install(context);

        using var tx = Connection.BeginTransaction();

        // Patch files
        foreach (var patchedFile in patchedFiles)
        {
            if (!group.Children.TryGetFirst(x => LoadoutFile.Hash.TryGetValue(x, out var hash) && hash == patchedFile.OriginalFileHashes.XxHash3, out var fileToPatch))
            {
                Logger.LogWarning("Unable to find original file of patched file `{Path}` by hash `{OriginalHash}` in loadout group", patchedFile.FileName, patchedFile.OriginalFileHashes);
                continue;
            }

            tx.Add(fileToPatch.Id, LoadoutFile.Hash, patchedFile.PatchedFileHashes.XxHash3);
            tx.Add(fileToPatch.Id, LoadoutFile.Size, patchedFile.PatchedFileHashes.Size);
        }

        // Add missing data from the collection to the item
        tx.Add(group.Id, LoadoutItem.Name, CollectionMod.Source.LogicalFilename ?? CollectionMod.Name);
        tx.Add(group.Id, NexusCollectionItemLoadoutGroup.Download, Item);
        tx.Add(group.Id, NexusCollectionItemLoadoutGroup.IsRequired, Item.IsRequired);

        var result = await tx.Commit();
        return new LoadoutItemGroup.ReadOnly(result.Db, group.Id);
    }

    private async ValueTask<(LoadoutItemGroup.ReadOnly, PatchedFile[])> Install(IJobContext<InstallCollectionDownloadJob> context)
    {
        if (Item.TryGetAsCollectionDownloadBundled(out var bundledDownload))
        {
            return (await InstallBundledMod(bundledDownload), []);
        }

        PatchedFile[] patchedFiles = [];

        var libraryFile = GetLibraryFile(Item, Connection.Db);
        if (CollectionMod.Patches.Count > 0)
        {
            if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive)) throw new NotSupportedException("Expected library file to be an archive");
            patchedFiles = await PatchFiles(libraryArchive, cancellationToken: context.CancellationToken);
        }

        if (CollectionMod.Hashes.Length > 0)
        {
            return (await InstallReplicatedMod(patchedFiles), []);
        }

        if (CollectionMod.Choices is { Type: ChoicesType.fomod })
        {
            return (await InstallFomodWithPredefinedChoices(context.CancellationToken), patchedFiles);
        }

        var result = await LoadoutManager.InstallItem(
            libraryFile.AsLibraryItem(),
            TargetLoadout,
            parent: Group.AsLoadoutItemGroup().LoadoutItemGroupId,
            // NOTE(erri120): https://github.com/Nexus-Mods/NexusMods.App/issues/2553
            // The advanced installer shouldn't appear when installing collections,
            // the decision was made that the app should behave similar to Vortex,
            // which installs unknown stuff into a "default folder"
            fallbackInstaller: FallbackInstaller
        );

        Debug.Assert(result.LoadoutItemGroup.HasValue);
        return (result.LoadoutItemGroup!.Value, patchedFiles);
    }

    private Task<LoadoutItemGroup.ReadOnly> InstallBundledMod(CollectionDownloadBundled.ReadOnly download) => LoadoutManager.InstallItemWrapper(TargetLoadout, tx =>
    {
        // Bundled mods are found inside the collection archive, so we'll have to find the files that are prefixed with the mod's source file expression.
        var prefixPath = RelativePath.FromUnsanitizedInput("bundled").Join(download.BundledPath);
        var prefixFiles = SourceCollectionArchive.Children.Where(f => f.Path.InFolder(prefixPath)).ToArray();

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

        return Task.FromResult(LoadoutItemGroupId.From(id.Value));
    });

    /// <summary>
    /// This is how we should get a fomod installer. We don't want to get it from DI or new it up, because
    /// the game may set custom target folders or other settings. So we'll have to get the game instance
    /// and find the installer that matches the type 
    /// </summary>
    private FomodXmlInstaller GetFomodXmlInstaller(CancellationToken cancellationToken)
    {
        var loadout = Loadout.Load(Connection.Db, TargetLoadout);
        var game = loadout.InstallationInstance.GetGame();
        var installer = game.LibraryItemInstallers.OfType<FomodXmlInstaller>().FirstOrDefault();
        
        return installer ?? throw new InvalidOperationException("FomodXmlInstaller not found");
    }

    /// <summary>
    /// Install a fomod with predefined choices.
    /// </summary>
    private Task<LoadoutItemGroup.ReadOnly> InstallFomodWithPredefinedChoices(CancellationToken cancellationToken) => LoadoutManager.InstallItemWrapper(TargetLoadout, async tx =>
    {
        var libraryFile = GetLibraryFile(Item, Connection.Db);
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive)) throw new NotSupportedException();

        var fomodInstaller = GetFomodXmlInstaller(cancellationToken);

        var loadoutItemGroup = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = Item.Name,
                LoadoutId = TargetLoadout,
                ParentId = Group.Id,
            },
        };

        _ = new NexusCollectionItemLoadoutGroup.New(tx, id)
        {
            DownloadId = Item,
            IsRequired = Item.IsRequired,
            LoadoutItemGroup = loadoutItemGroup,
        };

        var loadout = new Loadout.ReadOnly(Connection.Db, TargetLoadout);

        var options = CollectionMod.Choices!.Options;
        await fomodInstaller.ExecuteAsync(libraryArchive, loadoutItemGroup, tx, loadout, options, cancellationToken: cancellationToken);

        return loadoutItemGroup;
    });

    /// <summary>
    /// This sort of install is a bit strange. The Hashes field contains pairs of MD5 hashes and paths. The paths are
    /// the target locations of the mod files. The MD5 hashes are the hashes of the files. So it's a fromHash->toPath
    /// situation. We don't store the MD5 hashes in the database, so we'll have to calculate them on the fly.
    /// </summary>
    /// <param name="patchedFiles"></param>
    private Task<LoadoutItemGroup.ReadOnly> InstallReplicatedMod(PatchedFile[] patchedFiles) => LoadoutManager.InstallItemWrapper(TargetLoadout, async tx =>
    {
        // So collections hash everything by MD5, so we'll have to collect MD5 information for the files in the archive.
        // We don't do this during indexing into the library because this is the only case where we need MD5 hashes.
        ConcurrentDictionary<Md5Value, HashMapping> hashes = new();

        var libraryFile = GetLibraryFile(Item, Connection.Db);
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive))
            throw new NotImplementedException();

        await Parallel.ForEachAsync(libraryArchive.Children, async (child, token) =>
        {
            await using var stream = await FileStore.GetFileStream(child.AsLibraryFile().Hash, token);
            var md5 = await Md5Hasher.HashAsync(stream, cancellationToken: token);

            var file = child.AsLibraryFile();
            hashes[md5] = new HashMapping()
            {
                Hash = file.Hash,
                Size = file.Size,
            };
        });

        foreach (var patchedFile in patchedFiles)
        {
            if (!hashes.ContainsKey(patchedFile.OriginalFileHashes.Md5))
            {
                Logger.LogWarning("Archive doesn't contain a file matching the MD5 hash {MD5Hash} of a file to patch", patchedFile.OriginalFileHashes.Md5);
                continue;
            }

            var hashMapping = new HashMapping
            {
                Hash = patchedFile.PatchedFileHashes.XxHash3,
                Size = patchedFile.PatchedFileHashes.Size,
            };

            hashes[patchedFile.PatchedFileHashes.Md5] = hashMapping;
        }

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

        return group.Id;
    });

    private async ValueTask<PatchedFile[]> PatchFiles(LibraryArchive.ReadOnly srcArchive, CancellationToken cancellationToken)
    {
        var srcFiles = srcArchive.Children.ToFrozenDictionary(static x => x.Path, static x => x.AsLibraryFile());
        var collectionFiles = SourceCollectionArchive.Children.ToFrozenDictionary(static x => x.Path, static x => x.AsLibraryFile());

        var patches = CollectionMod.Patches.ToArray();
        var results = new ValueTuple<PatchedFile, MemoryStream>[patches.Length];

        await Parallel.ForAsync(fromInclusive: 0, toExclusive: patches.Length, cancellationToken: cancellationToken, async (i, innerCancellationToken) =>
        {
            var patch = patches[i];
            var result = await PatchFile(patch.Key, patch.Value, srcFiles, collectionFiles, cancellationToken: innerCancellationToken);
            results[i] = result;
        });

        var archivedFileEntries = results.Select(static tuple => new ArchivedFileEntry(
            StreamFactory: new MemoryStreamFactory(name: tuple.Item1.FileName, stream: tuple.Item2),
            Hash: tuple.Item1.PatchedFileHashes.XxHash3,
            Size: tuple.Item1.PatchedFileHashes.Size
        )).ToArray();

        await FileStore.BackupFiles(archivedFileEntries, deduplicate: true, token: cancellationToken);

        if (ApplicationConstants.IsDebug)
        {
            foreach (var result in results)
            {
                var (patchedFile, _) = result;
                var hash = patchedFile.PatchedFileHashes.XxHash3;
                var hasFile = await FileStore.HaveFile(hash);
                Debug.Assert(hasFile, "expected the file store to have the file it just backed up...");
            }
        }

        return results.Select(static x => x.Item1).ToArray();
    }

    private record struct PatchedFile(RelativePath FileName, MultiHash PatchedFileHashes, MultiHash OriginalFileHashes);

    private async ValueTask<(PatchedFile, MemoryStream)> PatchFile(
        RelativePath srcPath,
        Crc32Value expectedHash,
        FrozenDictionary<RelativePath, LibraryFile.ReadOnly> srcFiles,
        FrozenDictionary<RelativePath, LibraryFile.ReadOnly> collectionFiles,
        CancellationToken cancellationToken)
    {
        if (!srcFiles.TryGetValue(srcPath, out var srcFile)) throw new KeyNotFoundException($"Collection download archive doesn't contain file `{srcPath}`");

        var patchName = RelativePath.FromUnsanitizedInput($"patches/{CollectionMod.Name}/{srcPath}.diff");
        if (!collectionFiles.TryGetValue(patchName, out var patchFile)) throw new KeyNotFoundException($"Collection archive doesn't contain file `{patchName}`");

        var patchedFileStream = new MemoryStream(capacity: (int)srcFile.Size.Value);
        var (originalFileHashes, patchedFileHashes) = await PatchFile(fileToPatch: srcFile, patchDataFile: patchFile, expectedHash: expectedHash, outputStream: patchedFileStream, cancellationToken: cancellationToken);

        var patchedFile = new PatchedFile(
            FileName: srcPath,
            PatchedFileHashes: patchedFileHashes,
            OriginalFileHashes: originalFileHashes
        );

        Debug.Assert(Size.FromLong(patchedFileStream.Length) == patchedFileHashes.Size.Value);
        Logger.LogDebug("Patching result: `{PatchedFile}`", patchedFile);

        patchedFileStream.Position = 0;
        return (patchedFile, patchedFileStream);
    }

    private async ValueTask<(MultiHash OriginalFileHashes, MultiHash PatchedFileHashes)> PatchFile(LibraryFile.ReadOnly fileToPatch, LibraryFile.ReadOnly patchDataFile, Crc32Value expectedHash, Stream outputStream, CancellationToken cancellationToken)
    {
        await using var inputStream = await FileStore.GetFileStream(fileToPatch.Hash, token: cancellationToken);

        var originalFileHashes = await MultiHasher.HashStream(inputStream, cancellationToken: cancellationToken);
        if (originalFileHashes.Crc32 != expectedHash.Value) throw new InvalidOperationException("The source file's CRC32 hash does not match the expected hash.");

        inputStream.Position = 0;

        var patchData = await FileStore.Load(patchDataFile.Hash, token: cancellationToken);
        PatchFile(inputStream, patchData, outputStream);

        outputStream.Position = 0;
        var patchedFileHashes = await MultiHasher.HashStream(outputStream, cancellationToken: cancellationToken);

        return (originalFileHashes, patchedFileHashes);
    }

    private static void PatchFile(Stream inputStream, byte[] patchData, Stream outputStream)
    {
        // NOTE(erri120): This patching library is kinda ass, the API isn't async, they create this memory stream multiple times, and generally allocate a bunch of memory.
        // I wouldn't be surprised if this line will show up in memory and performance diagnosers.
        BsDiff.BinaryPatch.Apply(inputStream, openPatchStream: () => new MemoryStream(patchData, writable: false), outputStream);
    }

    private LibraryFile.ReadOnly GetLibraryFile(CollectionDownload.ReadOnly download, IDb db)
    {
        var status = CollectionDownloader.GetStatus(download, Group, db);
        if (status.IsInLibrary(out var libraryItem)) return GetLibraryFile(libraryItem, download);

        if (status.IsInstalled(out var loadoutItem))
        {
            var libraryLinkedLoadoutItem = LibraryLinkedLoadoutItem.Load(loadoutItem.Db, loadoutItem.Id);
            if (!libraryLinkedLoadoutItem.IsValid()) throw new NotSupportedException($"Expected loadout item `{loadoutItem.Name}` for download `{download.Name}` (index={download.ArrayIndex}) to be linked to a library item");
            return GetLibraryFile(libraryLinkedLoadoutItem.LibraryItem, download);
        }

        throw new NotSupportedException($"Status for download `{download.Name}` (index={download.ArrayIndex}) is {status.Value.Index}");
    }

    private static LibraryFile.ReadOnly GetLibraryFile(LibraryItem.ReadOnly libraryItem, CollectionDownload.ReadOnly download)
    {
        if (!libraryItem.TryGetAsLibraryFile(out var libraryFile))
            throw new NotSupportedException($"Expected library item `{libraryItem.Name}` for download `{download.Name}` (index={download.ArrayIndex}) to be a library file");
        return libraryFile;
    }
}
