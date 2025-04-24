using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games;

public interface ILoadoutSortableItemProvider : IDisposable
{
    /// <summary>
    /// The ISortableItemProviderFactory that created this provider
    /// </summary>
    public ISortableItemProviderFactory ParentFactory { get; }
    
    /// <summary>
    /// The id of the loadout that the sortable items are associated with
    /// </summary>
    public LoadoutId LoadoutId { get; }
    
    /// <summary>
    /// ChangeSet of sortable items in the sort order
    /// </summary>
    public IObservable<IChangeSet<ISortableItem, ISortItemKey>> SortableItemsChangeSet { get; }

    /// <summary>
    /// Returns the current sort order of the sortable items in the loadout.
    /// </summary>
    /// <returns>A read-only list of <c>ISortableItem</c> objects sorted in ascending sort index order</returns>
    public IReadOnlyList<ISortableItem> GetCurrentSorting();

    /// <summary>
    /// Returns the sortable item with the given id
    /// </summary>
    public Optional<ISortableItem> GetSortableItem(ISortItemKey itemId);

    /// <summary>
    /// Sets the relative position of a sortable item in the sort order
    /// </summary>
    /// <param name="sortableItem">item to move</param>
    /// <param name="delta">positive or negative index delta</param>
    /// <param name="token"></param>
    Task SetRelativePosition(ISortableItem sortableItem, int delta, CancellationToken token);

    /// <summary>
    /// Moves the given items to be before or after the target item in ascending index sort order.
    /// The relative index order of the moved items is preserved.
    /// Validity and outcome of the move may depend on game specific logic, so only some or none of the items may be moved.
    /// </summary>
    Task MoveItemsTo(ISortableItem[] sourceItems, ISortableItem targetItem, TargetRelativePosition relativePosition, CancellationToken token);

    /// <summary>
    /// Refreshes the sort order based on the data in the loadout and returns the updated sorted list.
    /// </summary>
    /// <param name="loadoutDb">An optional database revision from which to retrieve the latest loadout data, latest availably is used if none is provided</param>
    /// <param name="token">Cancellation token</param>
    /// <remarks>The computed load order is persisted, so passing outdated db revisions could result in loss of data.</remarks>
    Task<IReadOnlyList<ISortableItem>> RefreshSortOrder(CancellationToken token, IDb? loadoutDb = null);
}

/// <summary>
/// A loadout specific provider and manager of sortable items.
/// </summary>
public interface ILoadoutSortableItemProvider<TItem, TKey> : ILoadoutSortableItemProvider
    where TItem : ISortableItem<TItem, TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    /// <inheritdoc/>
    IObservable<IChangeSet<ISortableItem, ISortItemKey>> ILoadoutSortableItemProvider.SortableItemsChangeSet => SortableItemsChangeSet.ChangeKey((key, _) => (ISortItemKey)key).Transform(item => (ISortableItem)item);

    /// <inheritdoc cref="ILoadoutSortableItemProvider.SortableItemsChangeSet"/>
    new IObservable<IChangeSet<TItem, TKey>> SortableItemsChangeSet { get; }

    /// <inheritdoc/>
    IReadOnlyList<ISortableItem> ILoadoutSortableItemProvider.GetCurrentSorting() => GetCurrentSorting().Cast<ISortableItem>().ToList();
    /// <inheritdoc cref="ILoadoutSortableItemProvider.GetCurrentSorting"/>
    new IReadOnlyList<TItem> GetCurrentSorting();

    /// <inheritdoc/>
    Optional<ISortableItem> ILoadoutSortableItemProvider.GetSortableItem(ISortItemKey itemId) => GetSortableItem((TKey)itemId).Convert(x => (ISortableItem)x);
    /// <inheritdoc cref="ILoadoutSortableItemProvider.GetSortableItem"/>
    Optional<TItem> GetSortableItem(TKey itemId);

    /// <inheritdoc/>
    Task ILoadoutSortableItemProvider.SetRelativePosition(ISortableItem sortableItem, int delta, CancellationToken cancellation) => SetRelativePosition((TItem)sortableItem, delta, cancellation); 
    /// <inheritdoc cref="ILoadoutSortableItemProvider.SetRelativePosition"/>
    Task SetRelativePosition(TItem sortableItem, int delta, CancellationToken token);

    /// <inheritdoc/>
    Task ILoadoutSortableItemProvider.MoveItemsTo(ISortableItem[] sourceItems, ISortableItem targetItem, TargetRelativePosition relativePosition, CancellationToken token) => MoveItemsTo(sourceItems.Cast<TItem>().ToArray(), (TItem)targetItem, relativePosition, token);
    /// <inheritdoc cref="ILoadoutSortableItemProvider.MoveItemsTo"/>
    Task MoveItemsTo(TItem[] sourceItems, TItem targetItem, TargetRelativePosition relativePosition, CancellationToken token);

    /// <inheritdoc/>
    async Task<IReadOnlyList<ISortableItem>> ILoadoutSortableItemProvider.RefreshSortOrder(CancellationToken token, IDb? loadoutDb) => (await RefreshSortOrder(token, loadoutDb)).Cast<ISortableItem>().ToList();
    /// <inheritdoc cref="ILoadoutSortableItemProvider.RefreshSortOrder"/>
    new Task<IReadOnlyList<TItem>> RefreshSortOrder(CancellationToken token, IDb? loadoutDb = null);
}

/// <summary>
/// The position items should be moved to, relative to the target item in ascending index order.
/// </summary>
public enum TargetRelativePosition
{
    /// <summary>
    /// Items should be moved to be before the target item in ascending index order
    /// </summary>
    BeforeTarget,
    
    /// <summary>
    /// Items should be moved to be after the target item in ascending index order
    /// </summary>
    AfterTarget,
}
