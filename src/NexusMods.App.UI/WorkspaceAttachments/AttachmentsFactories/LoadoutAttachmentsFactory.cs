using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.App.UI.WorkspaceAttachments;

public class LoadoutAttachmentsFactory(IConnection conn) : IWorkspaceAttachmentsFactory<LoadoutContext>
{
    public string CreateTitle(LoadoutContext context)
    {
        // Use the game name as the title
        var loadout = Loadout.Load(conn.Db, context.LoadoutId);
        return loadout.InstallationInstance.Game.DisplayName;
    }

    public string CreateSubtitle(LoadoutContext context)
    {
        // Use the loadout name as the subtitle
        var loadout = Loadout.Load(conn.Db, context.LoadoutId);
        return loadout.Name;
    }
}
