
using DynamicData;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Rows;
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

    public static Query<(EntityId CollectionId, bool IsEnabled)> CollectionEnabledStateInLoadoutQuery(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId CollectionId, bool IsEnabled)>($"SELECT Id, IsEnabled FROM loadouts.CollectionEnabledState({connection}, {loadoutId})");
    }
    
    public static Query<(EntityId GroupId, bool IsEnabled)> LoadoutItemGroupEnabledStateInLoadoutQuery(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId GroupId, bool IsEnabled)>($"SELECT Id, IsEnabled FROM loadouts.ItemGroupEnabledState({connection}, {loadoutId})");
    }
    
    public static Query<(EntityId ItemId, bool IsEnabled)> LoadoutItemEnabledStateInLoadoutQuery(IConnection connection, LoadoutId loadoutId)
    {
        return connection.Query<(EntityId ItemId, bool IsEnabled)>($"SELECT Id, IsEnabled FROM loadouts.LoadoutItemEnabledState({connection}, {loadoutId})");
    }
    
    public static Query<(EntityId Id, GamePath TargetPath, Hash Hash, Size Size, bool IsDeleted)> EnabledLoadoutItemWithTargetPathInLoadoutQuery(IDb db, LoadoutId loadoutId)
    {
        return db.Connection.Query<(EntityId, GamePath, Hash, Size, bool)>($"SELECT * FROM loadouts.EnabledFilesWithMetadata({db}, {loadoutId})");
    }
}
