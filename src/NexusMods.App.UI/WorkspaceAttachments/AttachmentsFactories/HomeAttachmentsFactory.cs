using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WorkspaceAttachments;

public class HomeAttachmentsFactory : IWorkspaceAttachmentsFactory<HomeContext>
{
    public string CreateTitle(HomeContext context)
    {
        return Language.HomeWorkspace_Title;
    }
}
