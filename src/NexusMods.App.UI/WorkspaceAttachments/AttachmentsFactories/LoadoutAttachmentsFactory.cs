using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.WorkspaceAttachments;

public class LoadoutAttachmentsFactory(IConnection conn) : IWorkspaceAttachmentsFactory<LoadoutContext>
{
    public string CreateTitle(LoadoutContext context)
    {
        // Use the game name as the title
        var loadout = conn.Db.Get(context.LoadoutId);
        return loadout?.Installation.Game.Name ?? string.Empty;
    }
}
