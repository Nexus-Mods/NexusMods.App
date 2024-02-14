using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WorkspaceAttachments;

public class DownloadsAttachmentsFactory : IWorkspaceAttachmentsFactory<DownloadsContext>
{
    public string CreateTitle(DownloadsContext context)
    {
        return Language.Downloads_WorkspaceTitle;
    }
}
