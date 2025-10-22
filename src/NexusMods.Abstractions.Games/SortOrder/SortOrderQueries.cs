using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Abstractions.Games;

public static class SortOrderQueries
{
    /// <summary>
    /// Returns an observable of changes to loadoutItemsWithTargetPath in the specified game.
    /// </summary>
    public static IObservable<IChangeSet<(EntityId ItemId, EntityId GroupId, EntityId CollectionId, EntityId LoadoutId), EntityId>> 
        TrackLoadoutItemChanges(IConnection connection, GameId gameId)
    {
        return connection.Query<(EntityId ItemId, EntityId GroupId, EntityId CollectionId, EntityId LoadoutId)>($"""
                                                 SELECT * FROM sortorder.TrackLoadoutItemChanges({connection}, {gameId.Value})
                                                 """
            )
            .Observe(x => x.ItemId);
    }
}
