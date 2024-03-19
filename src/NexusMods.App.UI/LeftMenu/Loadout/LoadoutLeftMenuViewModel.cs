using System.Collections.ObjectModel;
using DynamicData.Kernel;
using NexusMods.App.UI.Controls;
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
                    var pageData = new PageData
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext { LoadoutId = loadoutContext.LoadoutId },
                    };

                    // TODO: use https://github.com/Nexus-Mods/NexusMods.App/issues/942
                    var input = NavigationInput.Default;

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, input, Optional<PageIdBundle>.None);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                }),
            },
            new IconViewModel
            {
                Name = Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics,
                Activate = ReactiveCommand.Create(() =>
                {
                    var pageData = new PageData
                    {
                        FactoryId = DummyPageFactory.StaticId,
                        Context = new DummyPageContext(),
                    };

                    // TODO: use https://github.com/Nexus-Mods/NexusMods.App/issues/942
                    var input = NavigationInput.Default;

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, input, Optional<PageIdBundle>.None);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                }),
            },
        };

        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
