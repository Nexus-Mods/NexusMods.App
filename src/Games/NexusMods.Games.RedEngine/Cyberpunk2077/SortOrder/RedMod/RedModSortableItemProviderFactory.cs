using System.ComponentModel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortableItemProviderFactory(IConnection connection) : ASortableItemProviderFactory<RedModSortableItem, SortItemKey<string>>(connection, Cyberpunk2077Game.GameIdStatic)
{
    private static readonly Guid StaticTypeId = new("9120C6F5-E0DD-4AD2-A99E-836F56796950");

    public override Guid SortOrderTypeId => StaticTypeId;

    public override string SortOrderName => "REDmod Load Order";

    public override string OverrideInfoTitle => "Load Order for REDmods in Cyberpunk 2077 - First Loaded Wins";

    public override string OverrideInfoMessage => """
                                         Some Cyberpunk 2077 mods use REDmods modules to alter core gameplay elements. If two REDmods modify the same part of the game, the one loaded first will take priority and overwrite changes from those loaded later.
                                         
                                         For example, the 1st position overwrites the 2nd, the 2nd overwrites the 3rd, and so on.
                                         """;

    public override string WinnerIndexToolTip => "First Loaded RedMOD Wins: Items that load first will overwrite changes from items loaded after them.";

    public override string IndexColumnHeader => "Load Order";

    public override string DisplayNameColumnHeader => "REDmod Name";
    public override string EmptyStateMessageTitle => "No REDmods detected";
    public override string EmptyStateMessageContents => "Some mods contain REDmod items that alter core gameplay elements. When detected, they will appear here for load order configuration.";
    public override string LearnMoreUrl => "https://nexus-mods.github.io/NexusMods.App/users/games/Cyberpunk2077/#redmod-load-ordering";

    public override ListSortDirection SortDirectionDefault => ListSortDirection.Ascending;

    public override IndexOverrideBehavior IndexOverrideBehavior => IndexOverrideBehavior.SmallerIndexWins;

    protected override async Task<ILoadoutSortableItemProvider<RedModSortableItem, SortItemKey<string>>> CreateProviderAsync(IConnection connection, LoadoutId currentLoadoutId)
    {
        return await RedModSortableItemProvider.CreateAsync(connection, currentLoadoutId, this);
    }
}
