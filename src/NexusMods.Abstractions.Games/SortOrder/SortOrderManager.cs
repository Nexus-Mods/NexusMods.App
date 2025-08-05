using System.Collections.Frozen;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
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
    public void RegisterSortOrderVarieties(ISortOrderVariety[] sortOrderVarieties)
    {
        _sortOrderVarieties = sortOrderVarieties.ToDictionary(variety => variety.SortOrderVarietyId)
            .ToFrozenDictionary();
        
        // Subscribe to changes in the sort orders
        SubscribeToChanges();
    }


    protected void SubscribeToChanges()
    {
        foreach (var variety in _sortOrderVarieties.Values)
        {
            throw new NotImplementedException();
            
            // TODO: Do initial cleanup
            // Remove orphaned sort orders
            // Create missing sort orders for existing loadouts/collections
            // Update existing sort orders to match the current state of the loadouts/collections
            //
            // variety.RefreshSortOrders();
        
            // TODO: Subscribe to loadouts/collections changes
            // Additions (add new sort orders for new loadouts/collections)
            // Updates (reconcile sort orders when loadouts/collections change)
            //     Skip updates for sort orders that don't want to be updated
            // Deletions (remove sort orders when loadouts/collections are deleted)
            //
            // variety.SubscribeToChanges();
        
            // TODO: Ensure subscription are disposed when a sort order is removed or manager is disposed
        }
        
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}


