using System.Collections.Concurrent;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Collections.Types;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
namespace NexusMods.Collections;

using ModInstructions = (Mod Mod, LibraryFile.ReadOnly LibraryFile);


public class InstallCollectionJob : IJobDefinitionWithStart<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>
{
    private IJobContext<InstallCollectionJob> _context = null!;
    public required NexusModsCollectionLibraryFile.ReadOnly SourceCollection { get; init; }
    
    public required IFileStore FileStore { get; init; }
    
    public required JsonSerializerOptions JsonSerializerOptions { get; init; }
    public required ILibraryService LibraryService { get; set; }
    public required IConnection Connection { get; set; }
    public required NexusModsLibrary NexusModsLibrary { get; set; }
    
    public required TemporaryFileManager TemporaryFileManager { get; set; }
    
    public required LoadoutId TargetLoadout { get; set; }




    public static IJobTask<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly> Create(IServiceProvider provider, LoadoutId target, NexusModsCollectionLibraryFile.ReadOnly source)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new InstallCollectionJob
        {
            TargetLoadout = target,
            SourceCollection = source,
            FileStore = provider.GetRequiredService<IFileStore>(),
            JsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>(),
            LibraryService = provider.GetRequiredService<ILibraryService>(),
            NexusModsLibrary = provider.GetRequiredService<NexusModsLibrary>(),
            Connection = provider.GetRequiredService<IConnection>(),
            TemporaryFileManager = provider.GetRequiredService<TemporaryFileManager>(),
        };
        return monitor.Begin<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly>(job);
    }



    public async ValueTask<NexusCollectionLoadoutGroup.ReadOnly> StartAsync(IJobContext<InstallCollectionJob> context)
    {
        _context = context;
        if (!SourceCollection.AsLibraryFile().TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The source collection is not a library archive.");
        
        var file = archive.Children.FirstOrDefault(f => f.Path == "collection.json");

        CollectionRoot? root;

        {
            await using var data = await FileStore.GetFileStream(file.AsLibraryFile().Hash);
            root = await JsonSerializer.DeserializeAsync<CollectionRoot>(data, JsonSerializerOptions);
        }
        
        if (root is null)
            throw new InvalidOperationException("Failed to deserialize the collection.json file.");

        ConcurrentBag<ModInstructions> toInstall = [];

        await Parallel.ForEachAsync(root.Mods, _context.CancellationToken, async (mod, _) => toInstall.Add(await EnsureDownloaded(mod)));

        using var tx = Connection.BeginTransaction();
        
        var group = new NexusCollectionLoadoutGroup.New(tx, out var id)
        {
            LibraryFileId= SourceCollection,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, id)
                {
                    Name = root.Info.Name,
                    LoadoutId = TargetLoadout,
                }
            }
            
        };
        var groupResult = await tx.Commit();
        
        await Parallel.ForEachAsync(toInstall, _context.CancellationToken, async (file, _) =>
            {
                // TODO: Implement FOMOD support
                if (file.Mod.Choices != null)
                    return;
                await InstallMod(TargetLoadout, file);
            }
        );
        
        return groupResult.Remap(group);
    }
    
    private async Task<LoadoutItemGroup.ReadOnly> InstallMod(LoadoutId loadoutId, ModInstructions file)
    {
        if (file.Mod.Hashes.Any())
            return await InstallReplicatedMod(loadoutId, file);
        
        return await LibraryService.InstallItem(file.LibraryFile.AsLibraryItem(), loadoutId);
    }

    /// <summary>
    /// This sort of install is a bit strange. The Hashes field contains pairs of MD5 hashes and paths. The paths are
    /// the target locations of the mod files. The MD5 hashes are the hashes of the files. So it's a fromHash->toPath
    /// situation. We don't store the MD5 hashes in the database, so we'll have to calculate them on the fly.
    /// </summary>
    private async Task<LoadoutItemGroup.ReadOnly> InstallReplicatedMod(LoadoutId loadoutId, ModInstructions file)
    {
        ConcurrentDictionary<Md5HashValue, LibraryFile.ReadOnly> hashes = new();
        
        if (!file.LibraryFile.TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The library file is not a library archive.");

        await Parallel.ForEachAsync(archive.Children, async (child, token) =>
            {
                await using var stream = await FileStore.GetFileStream(child.AsLibraryFile().Hash, token);
                using var hasher = System.Security.Cryptography.MD5.Create();
                var hash = await hasher.ComputeHashAsync(stream, token);
                var md5 = Md5HashValue.From(hash);

                hashes[md5] = child.AsLibraryFile();
            }
        );
        
        using var tx = Connection.BeginTransaction();
        
        // Create the group
        var group = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = file.Mod.Name,
                LoadoutId = loadoutId,
            },
        };
        
        // Link the group to the loadout
        _ = new LibraryLinkedLoadoutItem.New(tx, id)
        {
            LibraryItemId = file.LibraryFile.AsLibraryItem(),
            LoadoutItemGroup = group,
        };

        foreach (var pair in file.Mod.Hashes)
        {
            if (!hashes.TryGetValue(pair.MD5, out var libraryItem))
                throw new InvalidOperationException("The hash was not found in the archive.");

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

    private async Task<ModInstructions> EnsureDownloaded(Mod mod)
    {
        return mod.Source.Type switch
        {
            "nexus" => await EnsureNexusModDownloaded(mod),
            _ => throw new NotSupportedException($"The mod source type '{mod.Source.Type}' is not supported.")
        };
    }

    private async Task<ModInstructions> EnsureNexusModDownloaded(Mod mod)
    {
        var db = Connection.Db;
        var file = NexusModsFileMetadata.FindByFileId(db, mod.Source.FileId)
            .Where(f => f.ModPage.ModId == mod.Source.ModId)
            .FirstOrOptional(f => f.LibraryFiles.Any());

        if (file.HasValue)
            return (mod, file.Value.LibraryFiles.First().AsDownloadedFile().AsLibraryFile());

        await using var tempPath = TemporaryFileManager.CreateFile();

        var downloadJob = await NexusModsLibrary.CreateDownloadJob(tempPath, mod.DomainName, mod.Source.ModId, mod.Source.FileId);
        var libraryFile = await LibraryService.AddDownload(downloadJob);
        return (mod, libraryFile);
    }
}
