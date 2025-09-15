using System.Collections.Frozen;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using OneOf;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Implementation for ISortOrderManager
/// Responsible for subscribing and updating sortOrders for all registered varieties when loadouts or collections change.
/// </summary>
public class SortOrderManager : ISortOrderManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider; 
    private readonly IConnection _connection;
    private IDisposable? _subscription;
    
    private FrozenDictionary<SortOrderVarietyId, ISortOrderVariety> _sortOrderVarieties;

    public SortOrderManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connection = _serviceProvider.GetRequiredService<IConnection>();
        _sortOrderVarieties = FrozenDictionary<SortOrderVarietyId, ISortOrderVariety>.Empty;
    }

    /// <inheritdoc />
    public async ValueTask UpdateLoadOrders(LoadoutId loadoutId, Optional<CollectionGroupId> collectionGroupId = default, CancellationToken token = default)
    {
        var parentEntity = collectionGroupId.HasValue
            ? OneOf<LoadoutId, CollectionGroupId>.FromT1(collectionGroupId.Value)
            : OneOf<LoadoutId, CollectionGroupId>.FromT0(loadoutId);
        
        foreach (var sortOrderVariety in _sortOrderVarieties.Values)
        {
            // Reconcile the sort order for each variety
            var sortOrderId = await sortOrderVariety.GetOrCreateSortOrderFor(loadoutId, parentEntity, token);
            await sortOrderVariety.ReconcileSortOrder(sortOrderId, token: token);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ISortOrderVariety> GetSortOrderVarieties()
    {
        return _sortOrderVarieties.Values;
    }

    /// <inheritdoc />
    public void RegisterSortOrderVarieties(ISortOrderVariety[] sortOrderVarieties, IGame game)
    {
        _sortOrderVarieties = sortOrderVarieties.ToDictionary(variety => variety.SortOrderVarietyId)
            .ToFrozenDictionary();
        
        // Subscribe to changes in the sort orders
        SubscribeToChanges(game.GameId);
    }


    protected void SubscribeToChanges(GameId gameId)
    {
        var compositeDisposable = new CompositeDisposable();
        var conn = _connection;
        
        // Create/remove SortOrders for loadouts
        Loadout.ObserveAll(_connection)
            .StartWithEmpty()
            .FilterImmutable(l => l.Installation.GameId == gameId)
            .ToObservable()
            .SubscribeAwait(async (changes, token) =>
                {
                    foreach (var change in changes)
                    {
                        var loadoutId = change.Current.LoadoutId;
                        var parentEntity = loadoutId;

                        switch (change.Reason)
                        {
                            case ChangeReason.Add:
                                // Create the sort order for this loadout
                                // TODO: GetOrCreateSortOrderFor or UpdateLoadOrders
                                // Might not be needed if the other subscription to loadout items handles it
                                break;
                            case ChangeReason.Update:
                                // If loadout changes, we handle that in a separate subscription
                                break;
                            case ChangeReason.Remove:
                                // Remove the orphaned sort orders
                                // TODO
                                break;
                        }
                    }
                }
            )
            .AddTo(compositeDisposable);
        
        // Create/remove SortOrders for collection groups
        CollectionGroup.ObserveAll(_connection)
            .StartWithEmpty()
            .FilterImmutable(cg => cg.AsLoadoutItemGroup().AsLoadoutItem().Loadout.Installation.GameId == gameId)
            .ToObservable()
            .SubscribeAwait(async (changes, token) =>
                {
                    foreach (var change in changes)
                    {
                        var collectionGroupId = change.Current.CollectionGroupId;
                        var parentEntity = OneOf<LoadoutId, CollectionGroupId>.FromT1(collectionGroupId);

                        switch (change.Reason)
                        {
                            case ChangeReason.Add:
                                // Create the sort order for this collection group
                                // TODO: GetOrCreateSortOrderFor or UpdateLoadOrders
                                // Might not be needed if the other subscription to loadout items handles it
                                break;
                            case ChangeReason.Update:
                                // If collection group changes, we handle that in a separate subscription
                                break;
                            case ChangeReason.Remove:
                                // Remove the orphaned sort orders
                                // TODO
                                break;
                        }
                    }
                }
            )
            .AddTo(compositeDisposable);
        
        // For each changed loadout, reconcile the sort orders for that loadout
        // TODO: Move query somewhere else
        _connection.Query<(EntityId ChangedLoadout, TxId TxId)>($"""
                                                 SELECT item.Loadout, MAX(d.T) as tx
                                                 FROM mdb_LoadoutItem(Db=>{_connection}) item
                                                 JOIN mdb_Loadout(Db=>{_connection}) loadout on item.Loadout = loadout.Id
                                                 JOIN mdb_GameInstallMetadata(Db=>{_connection}) as install on loadout.Installation = install.Id
                                                 LEFT JOIN mdb_Datoms() d ON d.E = item.Id
                                                 WHERE install.GameId = {gameId.Value}
                                                 GROUP BY item.Loadout
                                                 """
            )
            .Observe(x => x.ChangedLoadout)
            .ToObservable()
            .SubscribeAwait(async (changes, token) =>
            {
                foreach (var change in changes)
                {
                    if (change.Reason != ChangeReason.Update)
                        continue;
                    
                    var changedLoadoutId = new LoadoutId(change.Key);
                    var txId = change.Current.TxId;
                    // TODO: This is likely wrong, we need to use this DB to get the loadout data, but the latest DB to get the sort order data
                    var referenceDb = _connection.AsOf(txId);

                    await UpdateLoadOrders(changedLoadoutId, token: token);
                }
               
            })
            .AddTo(compositeDisposable);
        
        // For each changed collection, reconcile the sort orders for that collection
        // TODO: Move query somewhere else
        _connection.Query<(EntityId ChangedCollection, EntityId LoaodutId, TxId TxId)>($"""
                                                 SELECT collection.Id, collection.Loadout, MAX(d.T) as tx
                                                 FROM mdb_CollectionGroup(Db=>{_connection}) collection
                                                 JOIN mdb_LoadoutItemGroup(Db=>{_connection}) itemGroup on itemGroup.Parent = collection.Id
                                                 JOIN mdb_LoadoutItem(Db=>{_connection}) item on item.Parent = itemGroup.Id
                                                 JOIN mdb_Loadout(Db=>{_connection}) loadout on collection.Loadout = loadout.Id
                                                 JOIN mdb_GameInstallMetadata(Db=>{_connection}) as install on loadout.Installation = install.Id
                                                 LEFT JOIN mdb_Datoms() d ON d.E = item.Id OR d.E = itemGroup.Id OR d.E = collection.Id
                                                 WHERE install.GameId = {gameId.Value}
                                                 GROUP BY collection.Id, collection.Loadout
                                                 """
            )
            .Observe(x => x.ChangedCollection)
            .ToObservable()
            .SubscribeAwait(async (changes, token) =>
            {
                foreach (var change in changes)
                {
                    if (change.Reason != ChangeReason.Update)
                        continue;
                    
                    var loadoutId = change.Current.LoaodutId;
                    var collectionId = new CollectionGroupId(change.Key);
                    var txId = change.Current.TxId;
                    var referenceDb = _connection.AsOf(txId);

                    await UpdateLoadOrders(loadoutId, collectionId, token: token);
                }
               
            })
            .AddTo(compositeDisposable);
        
        _subscription = compositeDisposable;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}


