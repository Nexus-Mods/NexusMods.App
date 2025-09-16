using DynamicData;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
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
    
    /// <summary>
    /// Resolves the name of a game based on its GameId.
    /// </summary>
    /// <param name="gameId">The GameId to resolve the name for.</param>
    /// <returns>The name of the game or a default string if not found.</returns>
    string ResolveGameName(GameId gameId);
}
