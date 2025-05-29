using DynamicData;
using DynamicData.Alias;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Cascade;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using OneOf;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortOrderVariety : ISortOrderVariety
{
    private readonly IConnection _connection;

    public RedModSortOrderVariety(IConnection connection)
    {
        _connection = connection;
    }
    
    public SortOrderId GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId> parentEntity)
    {
        throw new NotImplementedException();
    }

    public IObservable<IChangeSet<ISortableItem, ISortItemKey>> GetSortableItemsChangeSet(SortOrderId sortOrderId)
    {
        return _connection.Topology.Observe(Queries.RedModSortableItemsForSortOrder.Where(row => row.ParentSortOrderId.Equals(sortOrderId.Value)))
            .Transform(ISortableItem (tuple) => new NewRedModSortableItem(tuple.Key, tuple.SortIndex, tuple.ParentModName, tuple.IsEnabled))
            .AddKey(item => item.Key);
        
        return _connection.Topology.Observe(Queries.RedModSortableItemsForSortOrder.Where(row => row.ParentSortOrderId.Equals(sortOrderId.Value)))
            .Transform(ISortableItem (tuple) => new NewRedModSortableItem(tuple.Key, tuple.SortIndex, tuple.ParentModName, tuple.IsEnabled))
            .AddKey(item => item.Key);
    }

    public IReadOnlyList<ISortableItem> GetSortableItems(SortOrderId sortOrderId, IDb? db = null)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetSortOrder(SortOrderId sortOrderId, IReadOnlyList<ISortableItem> items, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask MoveItems(SortOrderId sortOrderId, ISortItemKey[] itemsToMove, ISortItemKey dropTargetItem, TargetRelativePosition relativePosition, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask MoveItemDelta(SortOrderId sortOrderId, ISortItemKey sourceItem, int delta, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask ReconcileSortOrder(SortOrderId sortOrderId, IDb? db = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
