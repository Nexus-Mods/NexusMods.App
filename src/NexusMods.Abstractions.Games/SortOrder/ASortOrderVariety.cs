using System.ComponentModel;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Abstract base class for a variety of sort order for a specific game.
/// </summary>
public abstract class ASortOrderVariety : ISortOrderVariety
{
    /// <inheritdoc />
    public abstract SortOrderVarietyId SortOrderVarietyId { get; }
    
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

    /// <inheritdoc />
    public virtual SortOrderUiMetadata SortOrderUiMetadata => StaticSortOrderUiMetadata;
    
    /// <inheritdoc />
    public virtual ListSortDirection SortDirectionDefault => ListSortDirection.Ascending;

    /// <inheritdoc />
    public virtual IndexOverrideBehavior IndexOverrideBehavior => IndexOverrideBehavior.GreaterIndexWins;

    /// <inheritdoc />
    public SortOrderId GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity)
    {
        throw new NotImplementedException();
        // TODO: Implement query to get SortOrder that has matching parent entity and matching SortOrderVarietyId
    }

    /// <inheritdoc />
    public abstract IObservable<IChangeSet<ISortableItem, ISortItemKey>> GetSortableItemsChangeSet(SortOrderId sortOrderId);

    /// <inheritdoc />
    public abstract IReadOnlyList<ISortableItem> GetSortableItems(SortOrderId sortOrderId, IDb? db = null);

    /// <inheritdoc />
    public ValueTask SetSortOrder(SortOrderId sortOrderId, IReadOnlyList<ISortItemKey> items, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask MoveItems(SortOrderId sortOrderId, ISortItemKey[] itemsToMove, ISortItemKey dropTargetItem, TargetRelativePosition relativePosition, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask MoveItemDelta(SortOrderId sortOrderId, ISortItemKey sourceItem, int delta, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
