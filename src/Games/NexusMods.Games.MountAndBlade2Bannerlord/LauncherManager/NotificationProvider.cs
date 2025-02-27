using Bannerlord.LauncherManager.External.UI;
using Bannerlord.LauncherManager.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;

internal class NotificationProvider : INotificationProvider
{
    public Task SendNotificationAsync(string id, NotificationType type, string message, uint displayMs)
    {
        return Task.CompletedTask;
        // This implementation has been stubbed out; it's only used to report borked SubModule.xml files in the game folder.
        // See a835fd43316905c06b35db8813af4dae3fb75fcd for last non-stubbed function with partial implementation.
        // Comment: https://github.com/Nexus-Mods/NexusMods.App/pull/2180#discussion_r1824783523
        // We will need to show a notification in the case of ingesting modules already in game folder,
        // in the case user downloaded one externally and we can't properly pick up on that.
    }
}
