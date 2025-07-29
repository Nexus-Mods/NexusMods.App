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
    
    private FrozenDictionary<SortOrderVarietyId, ISortOrderVariety> _sortOrderVarieties;

    public ASortOrderManager(IServiceProvider serviceProvider)
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
    }
}


