using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Collections;

public class DownloadCollectionJob : IJobDefinitionWithStart<DownloadCollectionJob, R3.Unit>
{
    public required ILogger<DownloadCollectionJob> Logger { get; init; }
    public required CollectionRevisionMetadata.ReadOnly RevisionMetadata { get; init; }
    public required CollectionDownloader.ItemType ItemType { get; init; }
    public required CollectionDownloader Downloader { get; init; }
    public required IDb Db { get; init; }
    public int MaxDegreeOfParallelism { get; init; } = -1;

    public async ValueTask<R3.Unit> StartAsync(IJobContext<DownloadCollectionJob> context)
    {
        var downloads = RevisionMetadata.Downloads.ToArray();
        
        await Parallel.ForAsync(fromInclusive: 0, toExclusive: downloads.Length, parallelOptions: new ParallelOptions
        {
            CancellationToken = context.CancellationToken,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism == -1 ? Environment.ProcessorCount : MaxDegreeOfParallelism,
        }, body: async (index, token) =>
        {
            var download = downloads[index];
            if (!CollectionDownloader.DownloadMatchesItemType(download, ItemType)) return;
            if (CollectionDownloader.GetStatus(download, Db).IsDownloaded()) return;

            try
            {
                if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
                {
                    await Downloader.Download(nexusModsDownload, token);
                }
                else if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
                {
                    await Downloader.Download(externalDownload, token);
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while downloading `{DownloadName}` from `{CollectionName}/{RevisionNumber}`", download.Name, download.CollectionRevision.Collection.Slug, download.CollectionRevision.RevisionNumber);
            }
        });

        return R3.Unit.Default;
    }
}
