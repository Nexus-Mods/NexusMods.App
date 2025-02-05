using NexusMods.Abstractions.NexusWebApi.Types.V2;
namespace NexusMods.Networking.ModUpdates;

/// <summary>
/// This is a helper struct which combines multiple <see cref="PerFeedCacheUpdater{TUpdateableItem}"/>
/// instances into a single interface. This allows for a cache update operation across
/// multiple mod feeds (games).
///
/// For usage instructions, see <see cref="PerFeedCacheUpdater{TUpdateableItem}"/>; the API and concepts
/// here are similar, except for the difference that this class' public API allows
/// you to use mods and API responses which are sourced from multiple feeds (games),
/// as opposed to a single feed.
/// </summary>
public class MultiFeedCacheUpdater<TUpdateableItem> where TUpdateableItem : IModFeedItem
{
    private readonly Dictionary<GameId, PerFeedCacheUpdater<TUpdateableItem>> _updaters;

    /// <summary>
    /// Creates a cache updater from a given list of items for which we want to
    /// check for updates.
    /// </summary>
    /// <param name="items">The items to check for updates.</param>
    /// <param name="expiry">
    ///     Maximum age before an item has to be re-checked for updates.
    ///     The max for Nexus Mods API is 1 month.
    /// </param>
    /// <remarks>
    ///     In order to ensure accuracy, the age field should include the lifespan of
    ///     the <see cref="PerFeedCacheUpdater{TUpdateableItem}"/> as well as the cache
    ///     time on the server's end. That should prevent any technical possible
    ///     eventual consistency errors due to race conditions. Although
    /// </remarks>
    public MultiFeedCacheUpdater(TUpdateableItem[] items, TimeSpan expiry)
    {
        _updaters = new Dictionary<GameId, PerFeedCacheUpdater<TUpdateableItem>>();
        
        // First group the items by their GameId
        // We will use a list of groups, the assumption being that the number
        // of feeds is low (usually less than 5)
        var groupedList = new List<(GameId, List<TUpdateableItem>)>();
        foreach (var item in items)
        {
            var gameId = item.GetModPageId().GameId;
            
            // Get or Update List for this GameId.
            var found = false;
            foreach (var (key, value) in groupedList)
            {
                if (key != gameId)
                    continue;

                value.Add(item);
                found = true;
                break;
            }

            if (!found)
                groupedList.Add((gameId, [item]));
        }

        // Create a PerFeedCacheUpdater for each group
        foreach (var (key, value) in groupedList)
            _updaters[key] = new PerFeedCacheUpdater<TUpdateableItem>(value.ToArray(), expiry);
    }

    /// <summary>
    /// Updates the internal state of the <see cref="PerFeedCacheUpdater{TUpdateableItem}"/>
    /// provided the results of the 'most recently updated mods for game' endpoint.
    /// </summary>
    /// <param name="items">
    /// The items returned by the 'most recently updated mods' endpoint. This can
    /// include items corresponding to multiple feeds (games); the feed source
    /// is automatically detected.
    ///
    /// Wrap elements in a struct that implements <see cref="IModFeedItem"/>
    /// and <see cref="IModFeedItem"/> if necessary. 
    /// </param>
    public void Update<T>(IEnumerable<T> items) where T : IModFeedItem
    {
        foreach (var item in items)
        {
            // Determine feed
            var feed = item.GetModPageId().GameId;
            
            // The result may contain items from feeds which we are not tracking.
            // For instance, results for other games. This is not an error, we
            // just need to filter the items out.
            if (!_updaters.TryGetValue(feed, out var updater))
                continue;
            
            updater.UpdateSingleItem(item);
        }
    }

    /// <summary>
    /// Determines the actions needed to taken on the items in the <see cref="MultiFeedCacheUpdater{TUpdateableItem}"/>;
    /// returning the items whose actions have to be taken grouped by the action that needs performed.
    ///
    /// The results of multiple feeds are flattened here; everything is returned as a single result.
    /// </summary>
    public PerFeedCacheUpdaterResult<TUpdateableItem> BuildFlattened(TimeSpan expiry)
    {
        var result = new PerFeedCacheUpdaterResult<TUpdateableItem>();
        foreach (var updater in _updaters)
            result.AddFrom(updater.Value.Build(expiry));

        return result;
    }
}
