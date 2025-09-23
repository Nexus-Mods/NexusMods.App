using DynamicData;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games;

public static class SortOrderQueries
{
    public static IObservable<IChangeSet<(EntityId CollectionId, EntityId LoadoutId, ulong TxId),EntityId>> TrackCollectionAndLoadoutChanges(IConnection connection, GameId gameId)
    {
        return connection.Query<(EntityId ChangedCollection, EntityId LoaodutId, ulong TxId)>($"""
                                                 SELECT * FROM sortorder.TrackCollectionAndLoadoutChanges({connection}, {gameId.Value})
                                                 """
            )
            .Observe(x => x.ChangedCollection);
    }
    
    public static IObservable<IChangeSet<(EntityId ItemId, EntityId GroupId, EntityId CollectionId, EntityId LoadoutId), EntityId>> TrackLoadoutItemChanges(IConnection connection, GameId gameId)
    {
        return connection.Query<(EntityId ItemId, EntityId GroupId, EntityId CollectionId, EntityId LoadoutId)>($"""
                                                 SELECT * FROM sortorder.TrackLoadoutItemChanges({connection}, {gameId.Value})
                                                 """
            )
            .Observe(x => x.ItemId);
    }
}
