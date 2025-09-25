using System.ComponentModel;
using System.Diagnostics.Contracts;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents a specific variety of sort order for a specific game.
/// Handles updating all the SortOrder entities of this variety.
/// One instance for each variety per game.
/// </summary>
/// <examples>
/// Cyberpunk RedMod load order;
/// Cyberpunk Archive load order;
/// Skyrim SE plugin load order;
/// </examples>
public interface ISortOrderVariety
{
    /// <summary>
    /// Returns an id identifying the variety of the sort order.
    /// </summary>
    SortOrderVarietyId SortOrderVarietyId { get; }
    
    /// <summary>
    /// Default direction (ascending/descending) in which sortIndexes should be sorted and displayed
    /// </summary>
    /// <remarks>
    /// Usually ascending, but could be different depending on what the community prefers and is used to
    /// </remarks>
    ListSortDirection SortDirectionDefault { get; }
    
    /// <summary>
    /// Defines whether smaller or greater index numbers win in case of conflicts between items in sorting order
    /// </summary>
    IndexOverrideBehavior IndexOverrideBehavior { get; }
    
    /// <summary>
    /// Contains UI strings and metadata for the sort order type
    /// </summary>
    SortOrderUiMetadata SortOrderUiMetadata { get; }
    
    /// <summary>
    /// Returns the SortOrderId for this variety for the given parent entity.
    /// </summary>
    [Pure]
    public Optional<SortOrderId> GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity);
    
    /// <summary>
    /// Returns the SortOrderId for the given loadout, parent entity and variety.
    /// If it does not exist, it will first be created.
    /// </summary>
    [Pure]
    public ValueTask<SortOrderId> GetOrCreateSortOrderFor(
        LoadoutId loadoutId,
        OneOf<LoadoutId, CollectionGroupId> parentEntity,
        CancellationToken token = default);
    
    /// <summary>
    /// Returns an observable change set of IReactiveSortItem for the given SortOrderId.
    /// </summary>
    [Pure]
    public IObservable<IChangeSet<IReactiveSortItem, ISortItemKey>> GetSortOrderItemsChangeSet(SortOrderId sortOrderId);
    
    /// <summary>
    /// Returns a list of IReactiveSortItem for the given SortOrderId.
    /// The latest database revision is used unless a specific IDb is provided.
    /// </summary>
    [Pure]
    public IReadOnlyList<IReactiveSortItem> GetSortOrderItems(SortOrderId sortOrderId, IDb? db = null);

    /// <summary>
    /// Moves the given items to be before or after the target item in ascending index sort order.
    /// The relative index order of the moved items is preserved.
    /// Validity and outcome of the move may depend on game-specific logic, so only some or none of the items may be moved.
    /// </summary>
    [Pure]
    public ValueTask MoveItems(
        SortOrderId sortOrderId,
        ISortItemKey[] itemsToMove,
        ISortItemKey dropTargetItem,
        TargetRelativePosition relativePosition,
        IDb? db = null,
        CancellationToken token = default);

    /// <summary>
    /// Sets the relative position of a sortable item in the sort order
    /// </summary>
    /// <param name="sourceItem">Key of the item to move</param>
    /// <param name="delta">positive or negative index delta</param>
    [Pure]
    public ValueTask MoveItemDelta(SortOrderId sortOrderId, ISortItemKey sourceItem, int delta, IDb? db = null, CancellationToken token = default);
    
    /// <summary>
    /// Reconcile the SortOrder with the latest data from the Db, adding or removing items as necessary.
    /// </summary>
    [Pure]
    public ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default);

    /// <summary>
    /// Deletes the SortOrder and all child SortOrderItems. 
    /// </summary>
    [Pure]
    public ValueTask DeleteSortOrder(SortOrderId sortOrderId, CancellationToken token = default);
    
    /// <summary>
    /// Static metadata for the sort order type that can be accessed by derived classes for reuse
    /// </summary>
    protected static SortOrderUiMetadata StaticSortOrderUiMetadata { get; } = new()
    {
        SortOrderName = "Load Order",
        OverrideInfoTitle = string.Empty,
        OverrideInfoMessage = string.Empty,
        WinnerIndexToolTip = "Last Loaded Mod Wins: Items that load last will overwrite changes from items loaded before them.",
        IndexColumnHeader = "LOAD ORDER",
        DisplayNameColumnHeader = "NAME",
        EmptyStateMessageTitle = "No Sortable Mods detected",
        EmptyStateMessageContents = "Some mods may modify the same game assets. When detected, they will be sortable via this interface.",
        LearnMoreUrl = string.Empty,
    };
}

/// <summary>
/// Genric version of <inheritdoc cref="ISortOrderVariety"/>, with implementation of non-generic methods.
/// To allow for type-safe access to the items in the implementations.
/// </summary>
public interface ISortOrderVariety<TKey, TItem> : ISortOrderVariety
    where TKey : IEquatable<TKey>, ISortItemKey
    where TItem : IReactiveSortItem<TItem, TKey>
{
    /// <inheritdoc/>
    [Pure]
    IObservable<IChangeSet<IReactiveSortItem, ISortItemKey>> ISortOrderVariety.GetSortOrderItemsChangeSet(SortOrderId sortOrderId) => 
        GetSortOrderItemsChangeSet(sortOrderId).ChangeKey((key,_) => (ISortItemKey)key).Transform(item => (IReactiveSortItem)item);
    
    /// <inheritdoc cref="ISortOrderVariety.GetSortOrderItemsChangeSet"/>
    [Pure]
    new IObservable<IChangeSet<TItem, TKey>> GetSortOrderItemsChangeSet(SortOrderId sortOrderId);
    
    /// <inheritdoc/>
    [Pure]
    IReadOnlyList<IReactiveSortItem> ISortOrderVariety.GetSortOrderItems(SortOrderId sortOrderId, IDb? db) => 
        GetSortOrderItems(sortOrderId, db).Cast<IReactiveSortItem>().ToList();
    
    /// <inheritdoc cref="ISortOrderVariety.GetSortOrderItems"/>
    [Pure]
    new IReadOnlyList<TItem> GetSortOrderItems(SortOrderId sortOrderId, IDb? db = null);
    
    /// <inheritdoc/>
    [Pure]
    ValueTask ISortOrderVariety.MoveItems(
        SortOrderId sortOrderId,
        ISortItemKey[] itemsToMove,
        ISortItemKey dropTargetItem,
        TargetRelativePosition relativePosition,
        IDb? db,
        CancellationToken token) =>
        MoveItems(sortOrderId, itemsToMove.Cast<TKey>().ToArray(), (TKey)dropTargetItem, relativePosition, db, token);
    
    /// <inheritdoc cref="ISortOrderVariety.MoveItems"/>
    [Pure]
    ValueTask MoveItems(
        SortOrderId sortOrderId,
        TKey[] itemsToMove,
        TKey dropTargetItem,
        TargetRelativePosition relativePosition,
        IDb? db = null,
        CancellationToken token = default);
    
    /// <inheritdoc/>
    [Pure]
    ValueTask ISortOrderVariety.MoveItemDelta(SortOrderId sortOrderId, ISortItemKey sourceItem, int delta, IDb? db, CancellationToken token) =>
        MoveItemDelta(sortOrderId, (TKey)sourceItem, delta, db, token);

    /// <inheritdoc cref="ISortOrderVariety.MoveItemDelta"/>
    [Pure]
    ValueTask MoveItemDelta(SortOrderId sortOrderId, TKey sourceItem, int delta, IDb? db = null, CancellationToken token = default);
}
