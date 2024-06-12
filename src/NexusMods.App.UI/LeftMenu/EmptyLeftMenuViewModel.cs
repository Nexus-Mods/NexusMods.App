using System.Collections.ObjectModel;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;


namespace NexusMods.App.UI.LeftMenu;

public class EmptyLeftMenuViewModel : AViewModel<ILeftMenuViewModel>, ILeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; }

    public EmptyLeftMenuViewModel(WorkspaceId workspaceId, string message)
    {
        WorkspaceId = workspaceId;

        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Icon = IconValues.Alert,
                Name = message,
            },
        };

        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
