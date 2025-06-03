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
    
    private readonly FrozenDictionary<SortOrderVarietyId, ISortOrderVariety> _sortOrderVarieties;

    public ASortOrderManager(IServiceProvider serviceProvider, ISortOrderVariety[] sortOrderVarieties)
    {
        _serviceProvider = serviceProvider;
        _connection = _serviceProvider.GetRequiredService<IConnection>();
        _logger = _serviceProvider.GetRequiredService<ILogger>();
        _sortOrderVarieties = sortOrderVarieties.ToDictionary(variety => variety.SortOrderVarietyId)
            .ToFrozenDictionary();
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
        
        var entities = SortOrder.FindByParentEntity(_connection.Db, parentEntity);
        
        foreach (var entity in entities)
        {
            var sortOrderVarietyId = SortOrderVarietyId.From(entity.SortOrderTypeId);
            
            if (_sortOrderVarieties.TryGetValue(sortOrderVarietyId, out var sortOrderVariety))
            {
                await sortOrderVariety.ReconcileSortOrder(entity.SortOrderId, token: token);
            }
            else
            {
                _logger.LogWarning("Found a sort order entity {SortOrderId} with unknown SortOrderVarietyId {SortOrderVarietyId}", entity.SortOrderId, sortOrderVarietyId);
                continue;
            }
        }
    }

    /// <inheritdoc />
    public ReadOnlySpan<ISortOrderVariety> GetSortOrderVarieties()
    {
        return _sortOrderVarieties.Values.AsSpan();
    }
}
