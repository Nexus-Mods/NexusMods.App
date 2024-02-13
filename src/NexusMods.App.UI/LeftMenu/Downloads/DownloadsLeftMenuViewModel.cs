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
    private readonly WorkspaceId _workspaceId;
    private readonly IWorkspaceController _workspaceController;
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    public DownloadsLeftMenuViewModel(IServiceProvider serviceProvider, WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        _workspaceController = workspaceController;
        _workspaceId = workspaceId;
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.InProgressTitleTextBlock, Icon = IconType.None,
                Activate = ReactiveCommand.Create(() =>
                {
                    _workspaceController.OpenPage(_workspaceId,
                        new PageData
                        {
                            FactoryId = InProgressPageFactory.StaticId,
                            Context = new InProgressPageContext()
                        },
                        new OpenPageBehavior(new OpenPageBehavior.PrimaryDefault()));
                })
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
