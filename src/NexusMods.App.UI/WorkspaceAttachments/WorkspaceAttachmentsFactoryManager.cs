using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WorkspaceAttachments;

public class WorkspaceAttachmentsFactoryManager(IEnumerable<ILeftMenuFactory> leftMenuFactories, IEnumerable<IWorkspaceAttachmentsFactory> attachmentsFactories)
    : IWorkspaceAttachmentsFactoryManager
{
    private ILeftMenuFactory[] LeftMenuFactories { get; } = leftMenuFactories.ToArray();
    private IWorkspaceAttachmentsFactory[] AttachmentsFactories { get; } = attachmentsFactories.ToArray();

    public ILeftMenuViewModel? CreateLeftMenuFor(IWorkspaceContext context, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        return LeftMenuFactories
            .Select(f => f.CreateLeftMenu(context, workspaceId, workspaceController))
            .FirstOrDefault(f => f != null);
    }

    public string CreateTitleFor(IWorkspaceContext context)
    {
        return AttachmentsFactories
            .Select(f => f.CreateTitle(context))
            .FirstOrDefault(t => t != null) ?? string.Empty;
    }
}
