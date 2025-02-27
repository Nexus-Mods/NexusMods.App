using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Collections;

public class DownloadCollectionJob : IJobDefinitionWithStart<DownloadCollectionJob, R3.Unit>
{
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

            if (download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload))
            {
                await Downloader.Download(nexusModsDownload, token);
            } else if (download.TryGetAsCollectionDownloadExternal(out var externalDownload))
            {
                await Downloader.Download(externalDownload, token);
            }
        });

        return R3.Unit.Default;
    }
}
