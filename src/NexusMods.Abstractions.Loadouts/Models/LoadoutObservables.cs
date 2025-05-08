using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts.Rows;
using NexusMods.Cascade;
using NexusMods.Cascade.Flows;
using NexusMods.Cascade.Patterns;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;

namespace NexusMods.Abstractions.Loadouts;


public partial class Loadout
{
    /// <summary>
    ///  Returns all items in a loadout
    /// </summary>
    private static readonly Flow<(EntityId Loadout, EntityId Entity)> LoadoutItemsSubFlow =
        Pattern.Create()
            .Db(out var loadoutItem, LoadoutItem.LoadoutId, out var loadoutId)
            .Return(loadoutId, loadoutItem);

    /// <summary>
    /// Include all sort order entities that are associated with a loadout
    /// </summary>
    private static readonly Flow<(EntityId Loadout, EntityId Entity)> LoadoutSortOrderSubFlow =
        Pattern.Create()
            .Db(out var sortItem, SortOrder.LoadoutId, out var loadoutId)
            .Return(loadoutId, sortItem);
    
    /// <summary>
    /// Include the loadout itself
    /// </summary>
    private static readonly Flow<(EntityId Loadout, EntityId LoadoutEntity)> LoadoutEntitySubFlow =
        Pattern.Create()
            .Db(out var loadoutEntity, Loadout.Name, out _)
            .Return(loadoutEntity, loadoutEntity);

    /// <summary>
    /// A union of all the entities associated with a loadout. The result of this query is a tuple of the loadout id and the entity id of the
    /// most recent transaction for that entity. If more entities need to be tracked, add another .With() call to this flow.
    /// </summary>
    public static readonly UnionFlow<(EntityId Loadout, EntityId Entity)> LoadoutAssociatedEntities =
        new UnionFlow<(EntityId Loadout, EntityId Entity)>(LoadoutEntitySubFlow)
            .With(LoadoutSortOrderSubFlow)
            .With(LoadoutItemsSubFlow);


    /// <summary>
    /// Calculates the most recent transaction for a loadout. Pretty simple, we just group by the loadout id and take the max transaction id.
    /// But this means that the row updates whenever any of the tracked entities are updated, meaning we can simply watch this row or the specific
    /// cell on the row to know whenever something modifies the loadout.
    /// </summary>
    public static readonly Flow<MostRecentTxForLoadoutRow.Active> MostRecentTxForLoadout =
        Pattern.Create()
            .Match(LoadoutAssociatedEntities, out var loadout, out var entity)
            .DbLatestTx(entity, out var maxTx)
            .ReturnMostRecentTxForLoadoutRow(loadout, maxTx.Max())
            .ToActive();
    
    
    /// <summary>
    /// Returns an IObservable of a loadout with the given id, refreshing it whenever a child entity is updated.
    /// </summary>
    public static IObservable<Loadout.ReadOnly> RevisionsWithChildUpdates(IConnection connection, LoadoutId id)
    {
        // A bit wordy, to have to specify all the generic types here, eventually we'll add a simpler method via Cascade to do this. 
        return connection.Topology
            .ObserveCell<MostRecentTxForLoadoutRow.Active, MostRecentTxForLoadoutRow, EntityId, EntityId>(MostRecentTxForLoadout, id, x => x.TxId)
            .Select(_ => Loadout.Load(connection.Db, id));
    }
}
