using System.Collections.ObjectModel;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

[UsedImplicitly]
public class DownloadsLeftMenuViewModel : AViewModel<IDownloadsLeftMenuViewModel>, IDownloadsLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; }

    public DownloadsLeftMenuViewModel(IServiceProvider serviceProvider, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        WorkspaceId = workspaceId;

        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.InProgressTitleTextBlock,
                Activate = ReactiveCommand.Create(() =>
                {
                    var pageData = new PageData
                    {
                        FactoryId = InProgressPageFactory.StaticId,
                        Context = new InProgressPageContext(),
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
