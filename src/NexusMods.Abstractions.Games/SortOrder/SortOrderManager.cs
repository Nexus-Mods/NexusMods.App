using System.Collections.Frozen;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger _logger;
    private IDisposable? _subscription;
    
    private FrozenDictionary<SortOrderVarietyId, ISortOrderVariety> _sortOrderVarieties;

    public SortOrderManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connection = _serviceProvider.GetRequiredService<IConnection>();
        _logger = _serviceProvider.GetRequiredService<ILogger>();
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
    public ReadOnlySpan<ISortOrderVariety> GetSortOrderVarieties()
    {
        return _sortOrderVarieties.Values.AsSpan();
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
        
        // TODO: Create observed query to detect changes to all game loadout contents, and returns the changed loadouts and collections
        
        // For each changed loadout, reconcile the sort orders for that loadout
        _connection.Query<(EntityId ChangedLoadout, TxId TxId)>($$"""
                                                 SELECT item.LoadoutId, MAX(d.T) as tx
                                                 FROM mdb_LoadoutItem(Db=>{Connection}) item
                                                 JOIN mdb_Loadout(Db=>{Connection}) loadout on item.LoadoutId = loadout.Id
                                                 JOIN mdb_GameInstallMetadata(Db=>{Connection}) as install on loadout.InstallationId = install.Id
                                                 LEFT JOIN mdb_Datoms() d ON d.E = item.Id
                                                 WHERE install.GameId = {gameId}
                                                 GROUP BY item.LoadoutId
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
        _connection.Query<(EntityId ChangedCollection, EntityId LoaodutId, TxId TxId)>($$"""
                                                 SELECT collection.Id, collection.LoadoutId, MAX(d.T) as tx
                                                 FROM mdb_CollectionGroup(Db=>{Connection}) collection
                                                 JOIN mdb_LoadoutItemGroup(Db=>{Connection}) itemGroup on itemGroup.Parent = collection.Id
                                                 JOIN mdb_LoadoutItem(Db=>{Connection}) item on item.Parent = itemGroup.Id
                                                 JOIN mdb_Loadout(Db=>{Connection}) loadout on collection.LoadoutId = loadout.Id
                                                 JOIN mdb_GameInstallMetadata(Db=>{Connection}) as install on loadout.InstallationId = install.Id
                                                 LEFT JOIN mdb_Datoms() d ON d.E = item.Id OR d.E = itemGroup.Id OR d.E = collection.Id
                                                 WHERE install.GameId = {gameId}
                                                 GROUP BY collection.Id
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


