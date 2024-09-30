using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi;

public class NexusModsDownloadJob : IDownloadJob, IJobDefinitionWithStart<NexusModsDownloadJob, AbsolutePath>
{
    public required IJobTask<HttpDownloadJob, AbsolutePath> HttpDownloadJob { get; init; }
    public required NexusModsFileMetadata.ReadOnly FileMetadata { get; init; }

    /// <inheritdoc/>
    public AbsolutePath Destination => HttpDownloadJob.Job.Destination;
    
    public static IJobTask<NexusModsDownloadJob, AbsolutePath> Create(IServiceProvider provider, IJobTask<HttpDownloadJob, AbsolutePath> httpDownloadJob, NexusModsFileMetadata.ReadOnly fileMetadata)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new NexusModsDownloadJob
        {
            HttpDownloadJob = httpDownloadJob,
            FileMetadata = fileMetadata,
        };
        return monitor.Begin<NexusModsDownloadJob, AbsolutePath>(job);
    }
    
    public async ValueTask<AbsolutePath> StartAsync(IJobContext<NexusModsDownloadJob> context)
    {
        return await HttpDownloadJob;
    }

    /// <inheritdoc/>
    public ValueTask AddMetadata(ITransaction tx, LibraryFile.New libraryFile)
    {
        libraryFile.GetLibraryItem(tx).Name = FileMetadata.Name;

        // Not using .New here because we can't use the LibraryItem Id and don't have the LibraryItem in this method
        tx.Add(libraryFile.Id, NexusModsLibraryItem.FileMetadataId, FileMetadata.Id);
        tx.Add(libraryFile.Id, NexusModsLibraryItem.ModPageMetadataId, FileMetadata.ModPage.Id);

        _ = new DownloadedFile.New(tx, libraryFile.Id)
        {
            DownloadPageUri = HttpDownloadJob.Job.DownloadPageUri,
            LibraryFile = libraryFile,
        };

        return ValueTask.CompletedTask;
    }

}
