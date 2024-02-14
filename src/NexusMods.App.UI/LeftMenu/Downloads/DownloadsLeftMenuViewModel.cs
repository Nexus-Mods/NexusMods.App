using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

[UsedImplicitly]
public class DownloadsLeftMenuViewModel : AViewModel<IDownloadsLeftMenuViewModel>, IDownloadsLeftMenuViewModel
{
    private readonly IWorkspaceController _workspaceController;
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; }

    public DownloadsLeftMenuViewModel(IServiceProvider serviceProvider, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        WorkspaceId = workspaceId;
        _workspaceController = workspaceController;
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.InProgressTitleTextBlock, Icon = IconType.None,
                Activate = ReactiveCommand.Create(() =>
                {
                    _workspaceController.OpenPage(WorkspaceId,
                        new PageData
                        {
                            FactoryId = InProgressPageFactory.StaticId,
                            Context = new InProgressPageContext()
                        },
                        _workspaceController.GetDefaultOpenPageBehavior());
                })
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
