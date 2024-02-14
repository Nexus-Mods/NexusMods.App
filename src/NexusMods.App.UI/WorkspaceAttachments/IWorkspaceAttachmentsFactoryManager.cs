using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WorkspaceAttachments;

public interface IWorkspaceAttachmentsFactoryManager
{
    public ILeftMenuViewModel? CreateLeftMenuFor(IWorkspaceContext context, WorkspaceId workspaceId, IWorkspaceController workspaceController);

    public string CreateTitleFor(IWorkspaceContext context);
}
