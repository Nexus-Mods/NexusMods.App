using Bannerlord.LauncherManager.External.UI;
using Bannerlord.LauncherManager.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;

internal class DialogProvider : IDialogProvider
{
    public void SendDialog(DialogType type, string title, string message, IReadOnlyList<DialogFileFilter> filters, Action<string> onResult)
    {
        onResult(string.Empty);
        // This implementation has been stubbed out; but will be needed for importing/saving load orders.
        // We need a system to forward messages to the UI via message passing, we currently don't have such a system.
        // See a835fd43316905c06b35db8813af4dae3fb75fcd for last non-stubbed function with partial implementation.
        // Comment: https://github.com/Nexus-Mods/NexusMods.App/pull/2180#discussion_r1824783523
    }
}
