using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Library;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// A Nexus Mods collection download job.
/// </summary>
public class NexusModsCollectionDownloadJob : IJobDefinitionWithStart<NexusModsCollectionDownloadJob, AbsolutePath>, IDownloadJob
{
    /// <summary>
    /// The download destination.
    /// </summary>
    public required AbsolutePath Destination { get; set; }

    /// <summary>
    /// The collection slug
    /// </summary>
    public required CollectionSlug Slug { get; set; }
    
    /// <summary>
    /// The revision number of the collection.
    /// </summary>
    public required RevisionNumber Revision { get; set; }
    
    /// <summary>
    /// Database connection.
    /// </summary>
    public required IConnection Connection { get; init; }
    
    /// <summary>
    /// The HTTP download job.
    /// </summary>
    public IJobTask<HttpDownloadJob, AbsolutePath>? DownloadJob { get; set; }
    
    /// <summary>
    /// Nexus Mods API client.
    /// </summary>
    public required INexusApiClient ApiClient { get; set; }


    /// <summary>
    /// The Nexus Mods library.
    /// </summary>
    public required NexusModsLibrary Library { get; set; }

    /// <summary>
    /// Service provider.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; set; }
    
    
    /// <summary>
    /// Creates a new <see cref="NexusModsCollectionDownloadJob"/> that downloads a collection file
    /// </summary>
    public static IJobTask<NexusModsCollectionDownloadJob, AbsolutePath> Create(IServiceProvider provider, CollectionSlug slug, RevisionNumber number, AbsolutePath destination)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new NexusModsCollectionDownloadJob
        {
            Slug = slug,
            Revision = number,
            Connection = provider.GetRequiredService<IConnection>(),
            Library = provider.GetRequiredService<NexusModsLibrary>(),
            ApiClient = provider.GetRequiredService<INexusApiClient>(),
            Destination = destination,
            ServiceProvider = provider,
        };
        return monitor.Begin<NexusModsCollectionDownloadJob, AbsolutePath>(job);
    }

    /// <inheritdoc />
    public ValueTask AddMetadata(ITransaction tx, LibraryFile.New libraryFile)
    {
        _ = new NexusModsCollectionLibraryFile.New(tx, libraryFile.Id)
        {
            CollectionSlug = Slug,
            CollectionRevisionNumber = Revision,
            LibraryFile = libraryFile,
        };
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask<AbsolutePath> StartAsync(IJobContext<NexusModsCollectionDownloadJob> context)
    {
        var downloadLinks = await ApiClient.CollectionDownloadLinksAsync(Slug, Revision, true, context.CancellationToken);
        DownloadJob = HttpDownloadJob.Create(ServiceProvider, downloadLinks.Data.DownloadLinks.First().Uri, downloadLinks.Data.DownloadLinks.First().Uri, Destination);
        return await DownloadJob;
    }
}
