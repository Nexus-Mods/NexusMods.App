using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WorkspaceAttachments;

public class LoadoutAttachmentsFactory : IWorkspaceAttachmentsFactory<LoadoutContext>
{
    private readonly ILoadoutRegistry _loadoutRegistry;

    public LoadoutAttachmentsFactory(ILoadoutRegistry loadoutRegistry)
    {
        _loadoutRegistry = loadoutRegistry;
    }


    public string CreateTitle(LoadoutContext context)
    {
        // Use the game name as the title
        var loadout = _loadoutRegistry.Get(context.LoadoutId);
        return loadout?.Installation.Game.Name ?? string.Empty;
    }
}
