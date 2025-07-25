using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.IO;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.Collections;

internal static class Verbs
{
    internal static IServiceCollection AddCollectionVerbs(this IServiceCollection collection) =>
        collection
#if DEBUG
            .AddVerb(() => GatherCollectionDefinitions)
            .AddVerb(() => CreateCollection)
#endif
            .AddVerb(() => InstallCollection);

    [Verb("install-collection", "Installs a collection into the given loadout")]
    private static async Task<int> InstallCollection([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to install the collection into")] Loadout.ReadOnly loadout,
        [Option("s", "slug", "Collection slug")] string slug,
        [Option("r", "revision", "Collection revision")] int revision,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] ILibraryService libraryService,
        [Injected] NexusModsLibrary nexusModsLibrary,
        [Injected] IServiceProvider serviceProvider,
        [Injected] ILoginManager loginManager,
        [Injected] IConnection connection,
        [Injected] CollectionDownloader collectionDownloader,
        [Injected] CancellationToken token)
    {
        await using var destination = temporaryFileManager.CreateFile();
        var downloadJob = nexusModsLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From(slug), RevisionNumber.From((ulong)revision), token);
        
        var libraryFile = await libraryService.AddDownload(downloadJob);

        if (!libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile))
            throw new InvalidOperationException("The library file is not a NexusModsCollectionLibraryFile");

        var revisionMetadata = await nexusModsLibrary.GetOrAddCollectionRevision(collectionFile, CollectionSlug.From(slug), RevisionNumber.From((ulong)revision), token);

        if (loginManager.IsPremium)
        {
            await collectionDownloader.DownloadItems(revisionMetadata, itemType: CollectionDownloader.ItemType.Required, db: connection.Db,
                cancellationToken: token
            );
        }
        else
        {
            var tuples = collectionDownloader.GetMissingDownloadLinks(revisionMetadata, db: connection.Db, itemType: CollectionDownloader.ItemType.Required);
            if (tuples.Count > 0)
            {
                await renderer.TextLine($"Missing {tuples.Count} downloads:");
                await renderer.Table(["Name", "Uri"], tuples.Select(t => new object[] { t.Download.Name, t.Uri.ToString() }));
                return 0;
            }
        }
        
        var items = CollectionDownloader.GetItems(revisionMetadata, CollectionDownloader.ItemType.Required);
        var installJob = await InstallCollectionJob.Create(serviceProvider, loadout, collectionFile, revisionMetadata, items);
        return 0;
    }
    
    /// <summary>
    /// This verb is only available in DEBUG builds, and is used to get large numbers of collection.json files. The code exists here incase we need
    /// this behavior again in the future. Disabled in release builds to prevent people from running it without knowing what it does. 
    /// </summary>
    /// <returns></returns>
    [Verb("gather-collection-definitions", "Downloads all the collection definitions for a given game, and extracts them to a folder")]
    private static async Task<int> GatherCollectionDefinitions([Injected] IRenderer renderer,
        [Option("g", "game", "Game to gather collection definitions for")] IGame game,
        [Option("o", "output", "Output folder")] AbsolutePath outputFolder,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] NexusModsLibrary nexusModsLibraryService,
        [Injected] INexusGraphQLClient nexusGraphQLClient,
        [Injected] IFileExtractor fileExtractor,
        [Injected] CancellationToken token)
    {
        var allCollections = await nexusGraphQLClient.CollectionsForGame.ExecuteAsync(game.GameId.ToString(), 0, 1000, token);
        var collections = allCollections.Data!.Collections.Nodes;
        
        await renderer.Text("Found {0} collections", collections.Count);

        foreach (var collection in collections)
        {
            var collFolder = outputFolder.Combine(collection.Slug + "_" + collection.LatestPublishedRevision!.RevisionNumber);
            if (collFolder.DirectoryExists())
                continue;
            await renderer.Text("Downloading {0}", collection.Name);
            await using var destination = temporaryFileManager.CreateFile();
            _ = await nexusModsLibraryService.CreateCollectionDownloadJob(destination, CollectionSlug.From(collection.Slug), RevisionNumber.From((ulong)collection.LatestPublishedRevision!.RevisionNumber), token);
            
            await renderer.Text("Extracting {0}", collection.Name);
            await fileExtractor.ExtractAllAsync(destination, collFolder, token);
        }

        return 0;
    }

    [Verb("create-collection", "Creates a collection")]
    private static async Task<int> CreateCollection(
        [Option("l", "loadout", "Loadout")] Loadout.ReadOnly loadout,
        [Injected] IServiceProvider serviceProvider,
        [Injected] CancellationToken cancellationToken)
    {
        var entityIds = loadout.Db.Datoms(
            (CollectionGroup.IsReadOnly, false),
            (LoadoutItem.LoadoutId, loadout)
        );

        if (!entityIds.TryGetFirst(out var collectionGroupId)) return -1;

        var collectionGroup = CollectionGroup.Load(loadout.Db, collectionGroupId);
        if (!collectionGroup.IsValid()) return -1;

        await CollectionCreator.UploadDraftRevision(serviceProvider, collectionGroupId, cancellationToken);
        return 0;
    }
}
