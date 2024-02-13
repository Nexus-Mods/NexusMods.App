using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuFactory(IServiceProvider serviceProvider) : ILeftMenuFactory<HomeContext>
{
    public ILeftMenuViewModel CreateLeftMenuViewModel(HomeContext context, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        return new HomeLeftMenuViewModel(serviceProvider.GetRequiredService<IMyGamesViewModel>(), workspaceId,
            workspaceController);
    }
}
