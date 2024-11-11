using System.Diagnostics;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using R3;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.LoadOrder;

public class RedModSortableItemProviderFactory : ISortableItemProviderFactory
{
    private readonly IConnection _connection;
    private readonly Dictionary<LoadoutId, RedModSortableItemProvider> _providers = new();

    public Guid StaticLoadOrderTypeId { get; } = new("9120C6F5-E0DD-4AD2-A99E-836F56796950");

    public RedModSortableItemProviderFactory(IConnection connection)
    {
        _connection = connection;

        Loadout.ObserveAll(_connection)
            .StartWithEmpty()
            .Filter(l => l.Installation.GameId == Cyberpunk2077Game.GameIdStatic)
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
                        if (_providers.TryGetValue(addition.Current.LoadoutId, out _))
                        {
                            // Provider already exists, should not happen
                            Debug.Assert(false, $"RedModSortableItemProviderFactory: provider already exists for loadout {addition.Current.LoadoutId}");
                            continue;
                        }

                        var provider = await RedModSortableItemProvider.CreateAsync(_connection, addition.Current.LoadoutId, this);
                        _providers.Add(addition.Current.LoadoutId, provider);
                    }


                    // Removals 
                    foreach (var removal in changes.Where(c => c.Reason == ChangeReason.Remove))
                    {
                        if (!_providers.Remove(removal.Current.LoadoutId, out var provider))
                        {
                            // Provider does not exist, should not happen
                            Debug.Assert(false, $"RedModSortableItemProviderFactory: provider not found for loadout {removal.Current.LoadoutId}");
                            continue;
                        }

                        // provider.Dispose();
                    }
                }
            );
    }


    public ILoadoutSortableItemProvider GetLoadoutSortableItemProvider(LoadoutId loadoutId)
    {
        if (_providers.TryGetValue(loadoutId, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"RedModSortableItemProviderFactory: provider not found for loadout {loadoutId}");
    }

}
