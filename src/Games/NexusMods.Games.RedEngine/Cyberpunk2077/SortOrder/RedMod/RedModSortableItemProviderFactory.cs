using System.ComponentModel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortableItemProviderFactory(IConnection connection) : ASortableItemProviderFactory(connection, Cyberpunk2077Game.GameIdStatic)
{
    private static readonly Guid StaticTypeId = new("9120C6F5-E0DD-4AD2-A99E-836F56796950");

    public override Guid SortOrderTypeId => StaticTypeId;

    public override SortOrderUiMetadata SortOrderUiMetadata { get; } = new()
    {
        SortOrderName = "REDmod Load Order",
        OverrideInfoTitle = "Load Order for REDmods in Cyberpunk 2077 - First Loaded Wins",
        OverrideInfoMessage = """
                              Some Cyberpunk 2077 mods use REDmods modules to alter core gameplay elements. If two REDmods modify the same part of the game, the one loaded first will take priority and overwrite changes from those loaded later.
                              For example, the 1st position overwrites the 2nd, the 2nd overwrites the 3rd, and so on.
                              """,
        WinnerIndexToolTip = "First Loaded RedMOD Wins: Items that load first will overwrite changes from items loaded after them.",
        IndexColumnHeader = "Load Order",
        DisplayNameColumnHeader = "REDmod Name",
        EmptyStateMessageTitle = "No REDmods detected",
        EmptyStateMessageContents = "Some mods contain REDmod items that alter core gameplay elements. When detected, they will appear here for load order configuration.",
        LearnMoreUrl = "https://nexus-mods.github.io/NexusMods.App/users/games/Cyberpunk2077/#redmod-load-ordering"
    };
    
    public override ListSortDirection SortDirectionDefault => ListSortDirection.Ascending;

    public override IndexOverrideBehavior IndexOverrideBehavior => IndexOverrideBehavior.SmallerIndexWins;
    
    public override async Task<ILoadoutSortableItemProvider> CreateProviderAsync(IConnection connection, LoadoutId currentLoadoutId)
    {
        return await RedModSortableItemProvider.CreateAsync(connection, currentLoadoutId, this);
    }
    
    
}
