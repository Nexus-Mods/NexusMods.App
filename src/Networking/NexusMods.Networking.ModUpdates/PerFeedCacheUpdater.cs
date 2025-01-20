using System.Diagnostics;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Networking.ModUpdates.Private;
namespace NexusMods.Networking.ModUpdates;

/// <summary>
/// This is a helper struct which helps us make use of the 'most recently updated mods for game'
/// API endpoint to use as a cache. (Where 'game' is a 'feed')
/// For full info on internals, see the NexusMods.App project documentation.
///
/// This API consists of the following:
///
/// 1. Input [Constructor]: A set of items with a 'last update time' and a 'unique id'
///                         (see <see cref="IModFeedItem"/>) that are relevant to the current 'feed' (game).
/// 
/// 2. Update [Method]: Submit results from API endpoint returning 'most recently updated mods for game'.
///           This updates the internal state of the <see cref="MultiFeedCacheUpdater{TUpdateableItem}"/>.
/// 
/// 3. Output [Info]: The <see cref="MultiFeedCacheUpdater{TUpdateableItem}"/> outputs items with 2 categories:
///        - Up-to-date mods. These should have their timestamp updated.
///        - Out of date mods. These require re-querying the data from external source.
/// </summary>
/// <remarks>
/// Within the Nexus Mods App:
///
/// - 'Input' is our set of locally cached mod pages.
/// - 'Update' is our results of `updated.json` for a given game domain.
/// - 'Output' are the pages we need to update.
///
/// The 'Feed' in the context of the Nexus App is the individual game's 'updated.json' endpoint;
/// i.e. a 'Game Mod Feed'
/// </remarks>
public class PerFeedCacheUpdater<TUpdateableItem> where TUpdateableItem : IModFeedItem
{
    private readonly TUpdateableItem[] _items;
    private readonly Dictionary<ModId, int> _itemToIndex;
    private readonly CacheUpdaterAction[] _actions;

    /// <summary>
    /// Creates a (per-feed) cache updater from a given list of items for which
    /// we want to check for updates.
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
    public PerFeedCacheUpdater(TUpdateableItem[] items, TimeSpan expiry)
    {
        _items = items;
        DebugVerifyAllItemsAreFromSameGame();

        _actions = new CacheUpdaterAction[items.Length];
        _itemToIndex = new Dictionary<ModId, int>(items.Length);
        for (var x = 0; x < _items.Length; x++)
            _itemToIndex[_items[x].GetModPageId().ModId] = x;

        // Set the action to refresh cache for any mods which exceed max age.
        var utcNow = DateTime.UtcNow;
        var minCachedDate = utcNow - expiry; 
        for (var x = 0; x < _items.Length; x++)
        {
            var lastUpdatedDate = _items[x].GetLastUpdatedDateUtc();
            if (lastUpdatedDate < minCachedDate)
                _actions[x] = CacheUpdaterAction.NeedsUpdate;
            else if (lastUpdatedDate >= minCachedDate)
                _actions[x] = CacheUpdaterAction.UpdateLastCheckedTimestamp;
        }
    }

    /// <summary>
    /// Updates the internal state of the <see cref="PerFeedCacheUpdater{TUpdateableItem}"/>
    /// provided the results of the 'most recently updated mods for game' endpoint.
    /// </summary>
    /// <param name="items">
    /// The items returned by the 'most recently updated mods for game' endpoint.
    /// Wrap elements in a struct that implements <see cref="IModFeedItem"/> if necessary.
    /// </param>
    public void Update<T>(IEnumerable<T> items) where T : IModFeedItem
    {
        foreach (var item in items)
            UpdateSingleItem(item);
    }
    
    internal void UpdateSingleItem<T>(T item) where T : IModFeedItem
    {
        // Try to get index of the item.
        // Not all the items from the update feed are locally stored, thus we need to
        // make sure we actually have this item.
        if (!_itemToIndex.TryGetValue(item.GetModPageId().ModId, out var index))
            return;
            
        var existingItem = _items[index];
            
        // If the file timestamp is newer than our cached copy, the item needs updating.
        if (item.GetLastUpdatedDateUtc() > existingItem.GetLastUpdatedDateUtc())
            _actions[index] = CacheUpdaterAction.NeedsUpdate;
        else
            _actions[index] = CacheUpdaterAction.UpdateLastCheckedTimestamp;
    }

    /// <summary>
    /// Determines the actions needed to taken on the items in the <see cref="PerFeedCacheUpdater{TUpdateableItem}"/>;
    /// returning the items whose actions have to be taken grouped by the action that needs performed.
    /// </summary>
    public PerFeedCacheUpdaterResult<TUpdateableItem> Build()
    {
        // We now have files in 3 categories:
        // - Up-to-date mods. (Determined in `Update` method) a.k.a. CacheUpdaterAction.UpdateLastCheckedTimestamp
        // - Out of date mods. (Determined in `Update` method and constructor) a.k.a. CacheUpdaterAction.NeedsUpdate
        // - Undetermined Mods. (Mods with CacheUpdaterAction.Default)
        //      - This is the case for mods that are not in the `updated.json` payload
        //        which for some reason are in our cache and are not out of date (not greater than expiry).
        //      - Alternatively, they are in our cache but not in the `updated.json` payload.
        //      - This can be the case if the expiry field is changed between calls, or the maxAge of the
        //        request whose results we feed to `Update` is inconsistent due to programmer error.
        //      - We return these in a separate category, but the consumer should treat them as 'Out of Date'
        var result = new PerFeedCacheUpdaterResult<TUpdateableItem>();
        for (var x = 0; x < _actions.Length; x++)
        {
            switch (_actions[x])
            {
                case CacheUpdaterAction.Default:
                    result.UndeterminedItems.Add(_items[x]);
                    break;
                case CacheUpdaterAction.NeedsUpdate:
                    result.OutOfDateItems.Add(_items[x]);
                    break;
                case CacheUpdaterAction.UpdateLastCheckedTimestamp:
                    result.UpToDateItems.Add(_items[x]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return result;
    }
    
    [Conditional("DEBUG")]
    private void DebugVerifyAllItemsAreFromSameGame()
    {
        if (_items.Length == 0) return;
        
        var firstGameId = _items[0].GetModPageId().GameId;
        var allSame = _items.All(x => x.GetModPageId().GameId == firstGameId);
        if (!allSame)
            throw new ArgumentException("All items must have the same game id", nameof(_items));
    }
}
