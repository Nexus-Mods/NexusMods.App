using System.Collections.Concurrent;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Json;
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
    
    public required Loadout.ReadOnly TargetLoadout { get; set; }




    public static IJobTask<InstallCollectionJob, NexusCollectionLoadoutGroup.ReadOnly> Create(IServiceProvider provider, Loadout.ReadOnly target, NexusModsCollectionLibraryFile.ReadOnly source)
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

        ConcurrentBag<LibraryFile.ReadOnly> toInstall = new();

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
        
        await Parallel.ForEachAsync(toInstall, _context.CancellationToken, async (file, _) => await InstallMod(TargetLoadout, file));
        
        return groupResult.Remap(group);
    }
    
    private async Task<LoadoutItemGroup.ReadOnly> InstallMod(Loadout.ReadOnly loadout, LibraryFile.ReadOnly file)
    {
        return await LibraryService.InstallItem(file.AsLibraryItem(), loadout.Id);
    }

    private async Task<LibraryFile.ReadOnly> EnsureDownloaded(Mod mod)
    {
        return mod.Source.Type switch
        {
            "nexus" => await EnsureNexusModDownloaded(mod),
            _ => throw new NotSupportedException($"The mod source type '{mod.Source.Type}' is not supported.")
        };
    }

    private async Task<LibraryFile.ReadOnly> EnsureNexusModDownloaded(Mod mod)
    {
        var db = Connection.Db;
        var file = NexusModsFileMetadata.FindByFileId(db, mod.Source.FileId)
            .Where(f => f.ModPage.ModId == mod.Source.ModId)
            .FirstOrOptional(f => f.LibraryFiles.Any());

        if (file.HasValue)
            return file.Value.LibraryFiles.First().AsDownloadedFile().AsLibraryFile();

        await using var tempPath = TemporaryFileManager.CreateFile();

        var downloadJob = await NexusModsLibrary.CreateDownloadJob(tempPath, mod.DomainName, mod.Source.ModId,
            mod.Source.FileId, _context.CancellationToken
        );
        var libraryFile = await LibraryService.AddDownload(downloadJob);
        return libraryFile;
    }
}
