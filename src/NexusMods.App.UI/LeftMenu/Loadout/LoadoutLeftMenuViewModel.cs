using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public ILaunchButtonViewModel LaunchButtonViewModel { get; }

    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; } = ReadOnlyObservableCollection<ILeftMenuItemViewModel>.Empty;

    public LoadoutLeftMenuViewModel(
        LoadoutContext loadoutContext,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutContext.LoadoutId;
    }
}
