using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Collections;

internal static class Verbs
{
    internal static IServiceCollection AddCollectionVerbs(this IServiceCollection collection) =>
        collection
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
        [Injected] CancellationToken token)
    {

        await using var destination = temporaryFileManager.CreateFile();
        var downloadJob = nexusModsLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From(slug), RevisionNumber.From((ulong)revision), token);
        
        var libraryFile = await libraryService.AddDownload(downloadJob);
        
        if (!libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile))
            throw new InvalidOperationException("The library file is not a NexusModsCollectionLibraryFile");

        var installJob = await InstallCollectionJob.Create(serviceProvider, loadout, collectionFile);

        return 0;
    }
}
