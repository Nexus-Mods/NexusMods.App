using System.Collections.ObjectModel;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
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
                Icon = IconValues.Downloading,
                NavigateCommand = ReactiveCommand.Create<NavigationInformation>(info =>
                {
                    var pageData = new PageData
                    {
                        FactoryId = InProgressPageFactory.StaticId,
                        Context = new InProgressPageContext(),
                    };

                    var behavior = workspaceController.GetOpenPageBehavior(pageData, info, Optional<PageIdBundle>.None);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                }),
            },
        };

        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
