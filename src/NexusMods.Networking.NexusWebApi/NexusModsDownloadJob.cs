using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using DynamicData.Kernel;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Models.Library;
using NexusMods.Sdk.Tracking;
using DownloadedFile = NexusMods.Sdk.Models.Library.DownloadedFile;

namespace NexusMods.Networking.NexusWebApi;

public class NexusModsDownloadJob : INexusModsDownloadJob, IJobDefinitionWithStart<NexusModsDownloadJob, AbsolutePath>
{
    public required ILogger Logger { private get; init; }
    public required IJobTask<IHttpDownloadJob, AbsolutePath> HttpDownloadJob { get; init; }
    public required NexusModsFileMetadata.ReadOnly FileMetadata { get; init; }
    public Optional<CollectionRevisionMetadata.ReadOnly> ParentRevision { get; init; }

    /// <inheritdoc/>
    public AbsolutePath Destination => HttpDownloadJob.JobDefinition.Destination;

    public static IJobTask<NexusModsDownloadJob, AbsolutePath> Create(
        IServiceProvider provider,
        IJobTask<HttpDownloadJob, AbsolutePath> httpDownloadJob,
        NexusModsFileMetadata.ReadOnly fileMetadata,
        Optional<CollectionRevisionMetadata.ReadOnly> parentRevision = default)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new NexusModsDownloadJob
        {
            Logger = provider.GetRequiredService<ILogger<NexusModsDownloadJob>>(),
            HttpDownloadJob = httpDownloadJob,
            FileMetadata = fileMetadata,
            ParentRevision = parentRevision,
        };

        return monitor.Begin<NexusModsDownloadJob, AbsolutePath>(job);
    }

    public async ValueTask<AbsolutePath> StartAsync(IJobContext<NexusModsDownloadJob> context)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var path = await HttpDownloadJob;

            Events.ModsDownloadCompleted(
                fileId: FileMetadata.Uid.FileId.Value,
                modId: FileMetadata.ModPage.Uid.ModId.Value,
                gameId: FileMetadata.Uid.GameId.Value,
                modUid: FileMetadata.ModPage.Uid.AsUlong,
                fileUid: FileMetadata.Uid.AsUlong,
                fileSize: FileMetadata.Size.ValueOrDefault(),
                duration: sw,
                collectionId: ParentRevision.Convert(x => x.Collection.CollectionId.Value),
                revisionId: ParentRevision.Convert(x => x.RevisionId.Value)
            );

            return path;
        }
        catch (TaskCanceledException)
        {
            Events.ModsDownloadCancelled(
                fileId: FileMetadata.Uid.FileId.Value,
                modId: FileMetadata.ModPage.Uid.ModId.Value,
                gameId: FileMetadata.Uid.GameId.Value,
                modUid: FileMetadata.ModPage.Uid.AsUlong,
                fileUid: FileMetadata.Uid.AsUlong,
                collectionId: ParentRevision.Convert(x => x.Collection.CollectionId.Value),
                revisionId: ParentRevision.Convert(x => x.RevisionId.Value)
            );

            // Propagate cancellation so upstream jobs (e.g. AddDownloadJob) can abort follow-up actions.
            Logger.LogInformation("Download cancelled by user for file `{GameId}/{ModId}/{FileId}`", FileMetadata.Uid.GameId, FileMetadata.ModPage.Uid.ModId, FileMetadata.Uid.FileId);
            throw;
        }
        catch (Exception e)
        {
            Events.ModsDownloadFailed(
                fileId: FileMetadata.Uid.FileId.Value,
                modId: FileMetadata.ModPage.Uid.ModId.Value,
                gameId: FileMetadata.Uid.GameId.Value,
                modUid: FileMetadata.ModPage.Uid.AsUlong,
                fileUid: FileMetadata.Uid.AsUlong,
                collectionId: ParentRevision.Convert(x => x.Collection.CollectionId.Value),
                revisionId: ParentRevision.Convert(x => x.RevisionId.Value)
            );

            Logger.LogError(e, "Exception while downloading file `{GameId}/{ModId}/{FileId}`", FileMetadata.Uid.GameId, FileMetadata.ModPage.Uid.ModId, FileMetadata.Uid.FileId);
            throw;
        }
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
            DownloadPageUri = HttpDownloadJob.JobDefinition.DownloadPageUri,
            LibraryFile = libraryFile,
        };

        return ValueTask.CompletedTask;
    }

}
