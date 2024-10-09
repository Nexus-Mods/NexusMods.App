using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text.Json;
using DynamicData.Kernel;
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
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Games.FOMOD;
using NexusMods.Hashing.xxHash64;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.V1Interop;
using NexusMods.Paths;
namespace NexusMods.Collections;

using ModInstructions = (Mod Mod, LibraryFile.ReadOnly LibraryFile);


public class InstallCollectionJob : IJobDefinitionWithStart<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>
{ 
    public required NexusModsCollectionLibraryFile.ReadOnly SourceCollection { get; init; }
    
    public required IFileStore FileStore { get; init; }
    
    public required JsonSerializerOptions JsonSerializerOptions { get; init; }
    public required ILibraryService LibraryService { get; set; }
    public required IConnection Connection { get; set; }
    public required NexusModsLibrary NexusModsLibrary { get; set; }
    
    public required TemporaryFileManager TemporaryFileManager { get; set; }
    
    public required LoadoutId TargetLoadout { get; set; }
    public required IServiceProvider SerivceProvider { get; set; }
    public required IGameDomainToGameIdMappingCache DomainMappingCache { get; set; }
    
    public static IJobTask<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly> Create(IServiceProvider provider, LoadoutId target, NexusModsCollectionLibraryFile.ReadOnly source)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new InstallCollectionJob
        {
            TargetLoadout = target,
            SourceCollection = source,
            SerivceProvider = provider,
            FileStore = provider.GetRequiredService<IFileStore>(),
            JsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>(),
            LibraryService = provider.GetRequiredService<ILibraryService>(),
            NexusModsLibrary = provider.GetRequiredService<NexusModsLibrary>(),
            Connection = provider.GetRequiredService<IConnection>(),
            TemporaryFileManager = provider.GetRequiredService<TemporaryFileManager>(),
            DomainMappingCache = provider.GetRequiredService<IGameDomainToGameIdMappingCache>(),
        };
        return monitor.Begin<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>(job);
    }
    
    /// <summary>
    /// Starts the install of a collection.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask<NexusCollectionLoadoutGroup.ReadOnly> StartAsync(IJobContext<InstallCollectionJob> context)
    {
        // Collections are essentially zip files with a collection.json file in them along with several other files.
        // So we can treat them as a library archive, and then extract the collection.json file from them.
        if (!SourceCollection.AsLibraryFile().TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The source collection is not a library archive.");
        
        // Find the collection.json file and deserialize it.
        var file = archive.Children.FirstOrDefault(f => f.Path == "collection.json");
        CollectionRoot? root;

        {
            await using var data = await FileStore.GetFileStream(file.AsLibraryFile().Hash);
            root = await JsonSerializer.DeserializeAsync<CollectionRoot>(data, JsonSerializerOptions);
        }
        
        if (root is null)
            throw new InvalidOperationException("Failed to deserialize the collection.json file.");

        // The collection.json file includes the mods by various ids and other info, so first we'll link those to library archives.
        ConcurrentBag<ModInstructions> toInstall = new();

        await Parallel.ForEachAsync(root.Mods, context.CancellationToken, async (mod, _) => toInstall.Add(await EnsureDownloaded(mod)));

        // Now create the collection in the loadout
        using var tx = Connection.BeginTransaction();
        
        var group = new NexusCollectionLoadoutGroup.New(tx, out var id)
        {
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
                    }
                }
            }
        };
        
        var groupResult = await tx.Commit();
        var groupRemapped = groupResult.Remap(group);
        
        // Now install the mods
        await Parallel.ForEachAsync(toInstall, context.CancellationToken, async (file, _) =>
            {
                // Bit strange, but Install Mod will want to find the collection group, so we'll have to rebase entity it will get the DB from
                file = (file.Mod, file.LibraryFile.Rebase());
                await InstallMod(TargetLoadout, file, groupRemapped.AsCollectionGroup().AsLoadoutItemGroup());
            }
        );
        
        return groupRemapped;
    }
    
    private async Task<LoadoutItemGroup.ReadOnly> InstallMod(LoadoutId loadoutId, ModInstructions file, LoadoutItemGroup.ReadOnly group)
    {
        // If the mod has a hashes entry, then we'll treat it as a replicated mod, and not use the library service to install it.
        if (file.Mod.Hashes.Any())
            return await InstallReplicatedMod(loadoutId, file, group);
        
        // If there are predefined fomod choices, then we'll use the FomodXmlInstaller to install it.
        if (file.Mod.Choices is { Type: ChoicesType.fomod })
            return await InstallFomodWithPredefinedChoices(loadoutId, file, group);

        // Otherwise, we'll just use the library service to install it.
        return await LibraryService.InstallItem(file.LibraryFile.AsLibraryItem(), loadoutId, parent: group.LoadoutItemGroupId);
    }

    /// <summary>
    /// Install a fomod with predefined choices.
    /// </summary>
    private async Task<LoadoutItemGroup.ReadOnly> InstallFomodWithPredefinedChoices(LoadoutId loadoutId, ModInstructions file, LoadoutItemGroup.ReadOnly collectionGroup)
    {
        // Get the archive from the library file
        if (!file.LibraryFile.TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The library file is not a library archive.");

        // Create the installer
        var fomodInstaller = FomodXmlInstaller.Create(SerivceProvider, new GamePath(LocationId.Game, ""));
        
        // Create the mod group and install the mod
        using var tx = Connection.BeginTransaction();
        var group = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = file.Mod.Name,
                LoadoutId = loadoutId,
                ParentId = collectionGroup.Id,
            },
        };
        var loadout = new Loadout.ReadOnly(Connection.Db, loadoutId);

        var options = file.Mod.Choices!.Options;
        await fomodInstaller.ExecuteAsync(archive, group, tx, loadout, options, CancellationToken.None);
        
        var result = await tx.Commit();
        
        return result.Remap(group);
    }

    /// <summary>
    /// This sort of install is a bit strange. The Hashes field contains pairs of MD5 hashes and paths. The paths are
    /// the target locations of the mod files. The MD5 hashes are the hashes of the files. So it's a fromHash->toPath
    /// situation. We don't store the MD5 hashes in the database, so we'll have to calculate them on the fly.
    /// </summary>
    private async Task<LoadoutItemGroup.ReadOnly> InstallReplicatedMod(LoadoutId loadoutId, ModInstructions file, LoadoutItemGroup.ReadOnly parentGroup)
    {
        // So collections hash everything by MD5, so we'll have to collect MD5 information for the files in the archive.
        // We don't do this during indexing into the library because this is the only case where we need MD5 hashes.
        ConcurrentDictionary<Md5HashValue, HashMapping> hashes = new();
        
        // Get the archive from the library file
        if (!file.LibraryFile.TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The library file is not a library archive.");
        
        // Get the collection archive
        if (!SourceCollection.AsLibraryFile().TryGetAsLibraryArchive(out var collectionArchive))
            throw new InvalidOperationException("The source collection is not a library archive.");

        // Hash all the files in the mod
        await Parallel.ForEachAsync(archive.Children, async (child, token) =>
            {
                await using var stream = await FileStore.GetFileStream(child.AsLibraryFile().Hash, token);
                using var hasher = MD5.Create();
                var hash = await hasher.ComputeHashAsync(stream, token);
                var md5 = Md5HashValue.From(hash);

                var libraryFile = child.AsLibraryFile();
                hashes[md5] = new HashMapping()
                {
                    Hash = libraryFile.Hash,
                    Size = libraryFile.Size,
                };
            }
        );
        
        // If we have any binary patching to do, then we'll do that now.
        if (file.Mod.Patches.Any()) 
            await PatchFiles(file.Mod, archive, collectionArchive, hashes);
        
        using var tx = Connection.BeginTransaction();
        
        // Create the group
        var group = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = file.Mod.Name,
                LoadoutId = loadoutId,
                ParentId = parentGroup.Id,
            },
        };
        
        // Link the group to the loadout
        _ = new LibraryLinkedLoadoutItem.New(tx, id)
        {
            LibraryItemId = file.LibraryFile.AsLibraryItem(),
            LoadoutItemGroup = group,
        };

        // Now we map the files to their locations based on the hashes
        foreach (var pair in file.Mod.Hashes)
        {
            // Try and find the hash we are looking for
            if (!hashes.TryGetValue(pair.MD5, out var libraryItem))
                throw new InvalidOperationException("The hash was not found in the archive.");

            // Map the file to the specific path
            var item = new LoadoutFile.New(tx, out var fileId)
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
    private async Task PatchFiles(Mod modInfo, LibraryArchive.ReadOnly modArchive, LibraryArchive.ReadOnly collectionArchive, 
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
                var xxHash = patchedArray.XxHash64();
                
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
    private Dictionary<RelativePath, LibraryFile.ReadOnly> IndexChildren(LibraryArchive.ReadOnly archive)
    {
        Dictionary<RelativePath, LibraryFile.ReadOnly> children = new();
        foreach (var child in archive.Children)
        {
            children[RelativePath.FromUnsanitizedInput(child.Path)] = child.AsLibraryFile();
        }

        return children;
    }

    private async Task<ModInstructions> EnsureDownloaded(Mod mod)
    {
        return mod.Source.Type switch
        {
            ModSourceType.nexus => await EnsureNexusModDownloaded(mod),
            _ => throw new NotSupportedException($"The mod source type '{mod.Source.Type}' is not supported.")
        };
    }

    private async Task<ModInstructions> EnsureNexusModDownloaded(Mod mod)
    {
        var db = Connection.Db;
        var gameId = (await DomainMappingCache.TryGetIdAsync(mod.DomainName, default(CancellationToken))).Value;
        var uid = new UidForFile(mod.Source.FileId, gameId);
        var file = NexusModsFileMetadata.FindByUid(db, uid)
            .Where(f => f.ModPage.Uid.ModId == mod.Source.ModId)
            .FirstOrOptional(f => f.LibraryFiles.Any());

        if (file.HasValue)
        {
            if (!file.Value.LibraryFiles.First().AsLibraryItem().TryGetAsLibraryFile(out var firstLibraryFile)) 
                return (mod, firstLibraryFile);
        }

        await using var tempPath = TemporaryFileManager.CreateFile();

        var downloadJob = await NexusModsLibrary.CreateDownloadJob(tempPath, gameId, mod.Source.ModId, mod.Source.FileId);
        var libraryFile = await LibraryService.AddDownload(downloadJob);
        return (mod, libraryFile);
    }
}
