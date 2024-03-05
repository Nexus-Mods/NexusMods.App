using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public IApplyControlViewModel ApplyControlViewModel { get; }


    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; }

    public LoadoutLeftMenuViewModel(
        LoadoutContext loadoutContext,
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        WorkspaceId = workspaceId;
        ApplyControlViewModel = new ApplyControlViewModel(loadoutContext.LoadoutId, serviceProvider);

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
                        workspaceController.GetDefaultOpenPageBehavior());
                })
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(items));
    }

}
