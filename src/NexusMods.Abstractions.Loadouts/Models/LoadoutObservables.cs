using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts;

public partial class Loadout
{
    
    /// <summary>
    /// Returns all mutable collection groups in a loadout.
    /// </summary>
    public static Query<(EntityId GroupId, string Name)> MutableCollections(IConnection connection, LoadoutId id) =>
        connection.Query<(EntityId, string)>($"SELECT Id, Name FROM mdb_CollectionGroup(Db=>{connection}) WHERE IsReadOnly = false AND Loadout = {id} ORDER BY Id");

    /// <summary>
    /// Returns an IObservable of a loadout with the given id, refreshing it whenever a child entity is updated.
    /// </summary>
    public static IObservable<Loadout.ReadOnly> RevisionsWithChildUpdates(IConnection connection, LoadoutId id)
    {
        return connection.Query<(EntityId LoadoutId, ulong Max, long Count)>($"""
                SELECT {id}, MAX(d.T), COUNT(d.E) FROM 
                loadouts.TrackedEntitiesForLoadout({connection}, {id}) ents
                LEFT JOIN mdb_Datoms() d ON d.E = ents.Id
                """)
            .Observe(x => x.LoadoutId)
            .QueryWhenChanged(_ => Loadout.Load(connection.Db, id));
    }
}
