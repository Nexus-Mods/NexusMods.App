using System.Collections.Frozen;
using System.Reactive.Linq;
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
    private IDisposable? _subscription;
    private readonly ILogger<SortOrderManager> _logger;
    
    private FrozenDictionary<SortOrderVarietyId, ISortOrderVariety> _sortOrderVarieties;

    public SortOrderManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connection = _serviceProvider.GetRequiredService<IConnection>();
        _sortOrderVarieties = FrozenDictionary<SortOrderVarietyId, ISortOrderVariety>.Empty;
        _logger = _serviceProvider.GetRequiredService<ILogger<SortOrderManager>>();
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
            .SubscribeAwait(static async (changes, token) =>
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
        
        // For each changed collection, reconcile the sort orders for that collection and for the parent loadout
        // TODO: Move query somewhere else
        // TODO: listen to changes to Game files too
        // TODO: this only checks for changes to items in collections, external changes is not covered
        SortOrderQueries.TrackCollectionAndLoadoutChanges(_connection, gameId)
            .ToObservable()
            .SubscribeAwait(this, static async (changes, state, token) =>
            {
                var loadouts = new HashSet<LoadoutId>();
                foreach (var change in changes)
                {
                    loadouts.Add(change.Current.LoadoutId);
                    
                    if (change.Reason != ChangeReason.Update)
                        continue;
                    
                    var loadoutId = change.Current.LoadoutId;
                    var collectionId = new CollectionGroupId(change.Key);
                
                    await state.UpdateLoadOrders(loadoutId, collectionId, token: token);
                }
                
                foreach (var loadoutId in loadouts)
                {
                    await state.UpdateLoadOrders(loadoutId, token: token);
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


