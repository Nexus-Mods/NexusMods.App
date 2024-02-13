using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public ILaunchButtonViewModel LaunchButtonViewModel { get; }

    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; }

    public LoadoutLeftMenuViewModel(
        LoadoutContext loadoutContext,
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        WorkspaceId = workspaceId;
        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutContext.LoadoutId;

        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.LoadoutLeftMenuViewModel_LoadoutGridEntry,
                Icon = IconType.None,
                Activate = ReactiveCommand.Create(() =>
                {
                    workspaceController.OpenPage(workspaceId,
                        new PageData
                        {
                            FactoryId = LoadoutGridPageFactory.StaticId,
                            Context = new LoadoutGridContext { LoadoutId = loadoutContext.LoadoutId }
                        },
                        new OpenPageBehavior(new OpenPageBehavior.PrimaryDefault()));
                })
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
