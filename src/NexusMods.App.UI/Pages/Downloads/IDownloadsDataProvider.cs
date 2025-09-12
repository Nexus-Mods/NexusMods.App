using DynamicData;
using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Controls;

namespace NexusMods.App.UI.Pages.Downloads;

/// <summary>
/// Data provider interface for downloads that transforms IDownloadsService data into CompositeItemModel collections.
/// </summary>
public interface IDownloadsDataProvider
{
    /// <summary>
    /// Observes downloads based on the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria for downloads.</param>
    /// <returns>Observable of download items as CompositeItemModel collections.</returns>
    IObservable<IChangeSet<CompositeItemModel<DownloadId>, DownloadId>> ObserveDownloads(DownloadsFilter filter);

    /// <summary>
    /// Observes the count of downloads based on the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria for downloads.</param>
    /// <returns>Observable of download count.</returns>
    IObservable<int> CountDownloads(DownloadsFilter filter);
}