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
        TrackLoadoutItemChanges(IConnection connection, NexusModsGameId nexusModsGameId)
    {
        return connection.Query<(EntityId ItemId, EntityId GroupId, EntityId CollectionId, EntityId LoadoutId)>($"""
                                                 SELECT * FROM sortorder.TrackLoadoutItemChanges({connection}, {nexusModsGameId.Value})
                                                 """
            )
            .Observe(x => x.ItemId);
    }
}
