using System.ComponentModel;
using System.Diagnostics;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using R3;

namespace NexusMods.Abstractions.Games;

/// <inheritdoc />
public abstract class ASortableItemProviderFactory<TItem, TKey> : ISortableItemProviderFactory
    where TItem : ISortableItem<TItem, TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    /// <inheritdoc />
    public Type ItemType => typeof(TItem);
    /// <inheritdoc />
    public Type KeyType => typeof(TKey);

    private readonly IConnection _connection;
    private readonly Dictionary<LoadoutId, ILoadoutSortableItemProvider<TItem, TKey>> _providers = new();
    
    /// <inheritdoc />
    public abstract Guid SortOrderTypeId { get; }

    /// <summary>
    /// Static metadata for the sort order type that can be accessed by derived classes for reuse
    /// </summary>
    protected static SortOrderUiMetadata StaticSortOrderUiMetadata { get; } = new()
    {
        SortOrderName = "Load Order",
        OverrideInfoTitle = string.Empty,
        OverrideInfoMessage = string.Empty,
        WinnerIndexToolTip = "Last Loaded Mod Wins: Items that load last will overwrite changes from items loaded before them.",
        IndexColumnHeader = "LOAD ORDER",
        DisplayNameColumnHeader = "NAME",
        EmptyStateMessageTitle = "No Sortable Mods detected",
        EmptyStateMessageContents = "Some mods may modify the same game assets. When detected, they will be sortable via this interface.",
        LearnMoreUrl = string.Empty,
    };

    /// <inheritdoc />
    public virtual SortOrderUiMetadata SortOrderUiMetadata => StaticSortOrderUiMetadata;

    /// <inheritdoc />
    public virtual ListSortDirection SortDirectionDefault => ListSortDirection.Ascending;

    /// <inheritdoc />
    public virtual IndexOverrideBehavior IndexOverrideBehavior => IndexOverrideBehavior.GreaterIndexWins;

    protected abstract Task<ILoadoutSortableItemProvider<TItem, TKey>> CreateProviderAsync(IConnection connection, LoadoutId currentLoadoutId);

    /// <summary>
    /// Constructor
    /// </summary>
    protected ASortableItemProviderFactory(
        IConnection connection,
        GameId gameId)
    {
        _connection = connection;
        
        Loadout.ObserveAll(connection)
            .StartWithEmpty()
            .Filter(l => l.Installation.GameId == gameId) 
            .ToObservable()
            .SubscribeAwait(async (changes, cancellationToken) =>
            {
                var additions = changes.Where(c => c.Reason == ChangeReason.Add).ToArray();
                var removals = changes.Where(c => c.Reason == ChangeReason.Remove).ToArray();

                if (additions.Length == 0 && removals.Length == 0)
                    return;

                // Additions
                foreach (var addition in additions)
                {
                    if (_providers.TryGetValue(addition.Current.LoadoutId, out var existing))
                    {
                        Debug.Assert(false, $"{existing.ToString()} exists for loadout {addition.Current.LoadoutId}");
                        continue;
                    }

                    var loadoutId = addition.Current.LoadoutId;
                    var provider = await CreateProviderAsync(connection, loadoutId);
                    if (!_providers.TryAdd(loadoutId, provider))
                    {
                        Debug.Assert(false, $"Provider already exists for loadout {loadoutId}");
                    }
                }

                // Removals
                foreach (var removal in removals)
                {
                    if (!_providers.Remove(removal.Current.LoadoutId, out var provider))
                    {
                        Debug.Assert(false, $"Provider not found for loadout {removal.Current.LoadoutId}");
                        continue;
                    }

                    provider.Dispose();
                }
            });
    }

    /// <inheritdoc />
    public virtual ILoadoutSortableItemProvider GetLoadoutSortableItemProvider(LoadoutId loadoutId)
    {
        if (_providers.TryGetValue(loadoutId, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"No provider exists to handle {loadoutId}");
    }

    private bool _isDisposed;
    
    /// <inheritdoc />
    public virtual void Dispose()
    {
        Dispose(true);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        
        if (disposing)
        {
            foreach (var provider in _providers.Values)
            {
                provider.Dispose();
                _providers.Clear();
            }
        }
        
        _isDisposed = true;
    }

}
