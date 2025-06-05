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
/// Abstract implementation for ISortOrderManager meant as the starting point for implementations.
/// </summary>
public class ASortOrderManager : ISortOrderManager
{
    private readonly IServiceProvider _serviceProvider; 
    private readonly IConnection _connection;
    private readonly ILogger _logger;
    
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(30);
    
    private FrozenDictionary<SortOrderVarietyId, ISortOrderVariety> _sortOrderVarieties;

    public ASortOrderManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connection = _serviceProvider.GetRequiredService<IConnection>();
        _logger = _serviceProvider.GetRequiredService<ILogger>();
        _sortOrderVarieties = FrozenDictionary<SortOrderVarietyId, ISortOrderVariety>.Empty;
    }

    /// <inheritdoc />
    public async ValueTask<IDisposable> Lock(CancellationToken token = default)
    {
        var hasEntered = await _semaphore.WaitAsync(_lockTimeout, token);
        if (hasEntered) return Disposable.Create(() => _semaphore.Release());
        
        // Failed to acquire the lock, check if cancellation was requested
        token.ThrowIfCancellationRequested();
        // Otherwise, throw a timeout exception
        throw new TimeoutException($"Failed to acquire lock after {_lockTimeout.TotalSeconds} seconds.");
    }

    /// <inheritdoc />
    public async ValueTask UpdateLoadOrders(LoadoutId loadoutId, Optional<CollectionGroupId> collectionGroupId = default, CancellationToken token = default)
    {
        var parentEntity = collectionGroupId.HasValue
            ? OneOf<LoadoutId, CollectionGroupId>.FromT1(collectionGroupId.Value)
            : OneOf<LoadoutId, CollectionGroupId>.FromT0(loadoutId);
        
        // Acquire the lock before proceeding with the reconciliation
        using var _ = await Lock(token);
        
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
    public void SetSortOrderVarieties(ISortOrderVariety[] sortOrderVarieties)
    {
        _sortOrderVarieties = sortOrderVarieties.ToDictionary(variety => variety.SortOrderVarietyId)
            .ToFrozenDictionary();
    }
}
