using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WorkspaceAttachments;

public class WorkspaceAttachmentsFactoryManager(IEnumerable<ILeftMenuFactory> leftMenuFactories)
    : IWorkspaceAttachmentsFactoryManager
{
    private ILeftMenuFactory[] AttachmentsFactories { get; } = leftMenuFactories.ToArray();


    public ILeftMenuViewModel? CreateLeftMenu(IWorkspaceContext context, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        return AttachmentsFactories
            .Select(f => f.CreateLeftMenu(context, workspaceId, workspaceController))
            .FirstOrDefault(f => f != null);
    }
}
