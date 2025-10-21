using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsLeftMenuFactory(IServiceProvider serviceProvider) : ILeftMenuFactory<DownloadsContext>
{
    public ILeftMenuViewModel CreateLeftMenuViewModel(DownloadsContext context, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        return new DownloadsLeftMenuViewModel(workspaceId, workspaceController, serviceProvider);
    }
}