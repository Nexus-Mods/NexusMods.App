using DynamicData;
using NexusMods.Abstractions.Loadouts.Rows;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Loadouts;


public partial class Loadout
{
    
    private const string TrackedEntitiesForLoadout = 
        """
        SELECT Id FROM mdb_LoadoutItem(Db=>$1) WHERE Loadout = $2
        UNION ALL
        SELECT Id FROM mdb_Loadout(Db=>$1) WHERE Id = $2
        UNION ALL
        SELECT sortItem.Id FROM mdb_SortOrder(Db=>$1) sortOrder
           LEFT JOIN mdb_SortOrderItem(Db=>$1) sortItem ON sortOrder.Id = sortItem.ParentSortOrder
           WHERE sortOrder.Loadout = $2
        """;

    private const string Revisions =
        $"""
        SELECT $2, MAX(d.T), COUNT(d.E) FROM 
        ({TrackedEntitiesForLoadout}) ents
        LEFT JOIN mdb_Datoms() d ON d.E = ents.Id
        """;
    
    /// <summary>
    /// Returns all mutable collection groups in a loadout.
    /// </summary>
    public static Query<(EntityId GroupId, string Name)> MutableCollections(IConnection connection, LoadoutId id) =>
        connection.Query<(EntityId, string)>("SELECT Id, Name FROM mdb_CollectionGroup(Db=>$1) WHERE IsReadOnly = false AND Loadout = $2 ORDER BY Id", connection, id.Value);
    

    /// <summary>
    /// Returns an IObservable of a loadout with the given id, refreshing it whenever a child entity is updated.
    /// </summary>
    public static IObservable<Loadout.ReadOnly> RevisionsWithChildUpdates(IConnection connection, LoadoutId id)
    {
        return connection.Query < (EntityId LoadoutID, ulong Max, long Count)>(Revisions, connection, id.Value)
            .Observe(x => x.LoadoutID)
            .QueryWhenChanged(_ => Loadout.Load(connection.Db, id));
    }
}
