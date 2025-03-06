using System.ComponentModel;
using System.Diagnostics;
using DynamicData;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using R3;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortableItemProviderFactory : ISortableItemProviderFactory
{
    private readonly IConnection _connection;
    private readonly Dictionary<LoadoutId, RedModSortableItemProvider> _providers = new();
    private static readonly Guid StaticTypeId = new("9120C6F5-E0DD-4AD2-A99E-836F56796950");

    public Guid SortOrderTypeId => StaticTypeId;

    public string SortOrderName => "REDmod Load Order";

    public string SortOrderHeading => "First Loaded REDmod Wins";

    public string OverrideInfoTitle => "Load Order for REDmods in Cyberpunk 2077 - First Loaded Wins";

    public string OverrideInfoMessage => """
                                         Some Cyberpunk 2077 mods use REDmods modules to alter core gameplay elements. If two REDmods modify the same part of the game, the one loaded first will take priority and overwrite changes from those loaded later.
                                         
                                         For example, the 1st position overwrites the 2nd, the 2nd overwrites the 3rd, and so on.
                                         """;
    public string WinnerIndexToolTip => "First Loaded RedMOD Wins: Items that load first will overwrite changes from items loaded after them."; 

    public string IndexColumnHeader => "LOAD ORDER";

    public string NameColumnHeader => "REDMOD NAME";
    
    public string EmptyStateMessageTitle => "No REDmods detected";
    public string EmptyStateMessageContents => "Some mods contain REDmod items that alter core gameplay elements. When detected, they will appear here for load order configuration.";

    public ListSortDirection SortDirectionDefault => ListSortDirection.Ascending;

    public IndexOverrideBehavior IndexOverrideBehavior => IndexOverrideBehavior.SmallerIndexWins;

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

                        // TODO: Delete SortOrder and SortableItem entities from DB if it isn't done in Synchronizer.DeleteLoadout()
                        provider.Dispose();
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
    
    public void Dispose()
    {
        foreach (var provider in _providers.Values)
        {
            provider.Dispose();
        }
    }
}
