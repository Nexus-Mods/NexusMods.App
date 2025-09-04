using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Extension methods for <see cref="IDownloadsService"/>.
/// </summary>
[PublicAPI]
public static class IDownloadsServiceExtensions
{
    /// <summary>
    /// Gets an observable of the average progress percent of all active downloads.
    /// </summary>
    public static IObservable<Percent> AverageProgressPercent(this IDownloadsService service)
    {
        return service.ActiveDownloads
            .AutoRefreshOnObservable(_ => Observable.Interval(TimeSpan.FromSeconds(1)))
            .QueryWhenChanged(items =>
            {
                if (items.Count == 0) return Percent.Zero;
                
                var totalProgress = items.Items.Sum(x => x.Progress.Value);
                return Percent.CreateClamped(totalProgress / items.Count);
            });
    }
    
    /// <summary>
    /// Gets an observable indicating if any downloads are active.
    /// </summary>
    public static IObservable<bool> HasActiveDownloads(this IDownloadsService service)
    {
        return service.ActiveDownloads
            .QueryWhenChanged(items => items.Count > 0);
    }
    
    /// <summary>
    /// Gets an observable of the count of active downloads.
    /// </summary>
    public static IObservable<int> ActiveDownloadCount(this IDownloadsService service)
    {
        return service.ActiveDownloads
            .QueryWhenChanged(items => items.Count);
    }
    
    /// <summary>
    /// Gets downloads that match a specific status.
    /// </summary>
    public static IObservable<IChangeSet<DownloadInfo, DownloadId>> GetDownloadsByStatus(
        this IDownloadsService service, 
        JobStatus status)
    {
        return service.AllDownloads
            .AutoRefresh(x => x.Status)
            .Filter(x => x.Status == status);
    }
    
    /// <summary>
    /// Gets failed downloads.
    /// </summary>
    public static IObservable<IChangeSet<DownloadInfo, DownloadId>> FailedDownloads(this IDownloadsService service)
    {
        return service.GetDownloadsByStatus(JobStatus.Failed);
    }
}