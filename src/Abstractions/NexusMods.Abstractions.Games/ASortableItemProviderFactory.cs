using System.ComponentModel;
using System.Diagnostics;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using R3;

namespace NexusMods.Abstractions.Games;

/// <inheritdoc />
public abstract class ASortableItemProviderFactory : ISortableItemProviderFactory
{
    private readonly IConnection _connection;
    private readonly Dictionary<LoadoutId, ILoadoutSortableItemProvider> _providers = new();
    
    /// <inheritdoc />
    public abstract Guid SortOrderTypeId { get; }
    
    /// <inheritdoc />
    public abstract string LearnMoreUrl { get; }
    
    /// <inheritdoc />
    public virtual string SortOrderName => "Load Order";
    
    /// <inheritdoc />
    public virtual string OverrideInfoMessage => string.Empty;

    /// <inheritdoc />
    public virtual string OverrideInfoTitle => string.Empty;
    
    /// <inheritdoc />
    public virtual string WinnerIndexToolTip => "Last Loaded Mod Wins: Items that load last will overwrite changes from items loaded before them.";

    /// <inheritdoc />
    public virtual string IndexColumnHeader => "LOAD ORDER";

    /// <inheritdoc />
    public virtual string DisplayNameColumnHeader => "NAME";

    /// <inheritdoc />
    public virtual string EmptyStateMessageTitle => "No Sortable Mods detected";

    /// <inheritdoc />
    public virtual string EmptyStateMessageContents => "Some mods may modify the same game assets. When detected, they will be sortable via this interface.";

    /// <inheritdoc />
    public virtual ListSortDirection SortDirectionDefault => ListSortDirection.Ascending;

    /// <inheritdoc />
    public virtual IndexOverrideBehavior IndexOverrideBehavior => IndexOverrideBehavior.GreaterIndexWins;

    
    public abstract Task<ILoadoutSortableItemProvider> CreateProviderAsync(IConnection connection, LoadoutId currentLoadoutId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="gameId"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected ASortableItemProviderFactory(
        IConnection connection,
        GameId gameId)
    {
        _connection = connection;
        var gameRegistry = connection.ServiceProvider.GetRequiredService<IGameRegistry>();
        var game = gameRegistry.Installations.Values
                .FirstOrDefault(x => x.Game.GameId.Equals(gameId))?.GetGame();
        if (game == null) return;
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
    
    /// <inheritdoc />
    public virtual void Dispose()
    {
        foreach (var provider in _providers.Values)
        {
            provider.Dispose();
        }

        _providers.Clear();
    }
}
