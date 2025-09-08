using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsLeftMenuDesignViewModel : AViewModel<IDownloadsLeftMenuViewModel>, IDownloadsLeftMenuViewModel
{
    public WorkspaceId WorkspaceId { get; } = WorkspaceId.NewId();
    
    public ILeftMenuItemViewModel LeftMenuItemAllDownloads { get; } = new LeftMenuItemDesignViewModel
    {
        Text = new StringComponent(Language.DownloadsLeftMenu_AllDownloads),
        Icon = IconValues.Download,
    };

    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> LeftMenuItemsPerGameDownloads { get; }

    public ILeftMenuItemViewModel LeftMenuItemAllCompleted { get; } = new LeftMenuItemDesignViewModel
    {
        Text = new StringComponent(Language.DownloadsLeftMenu_AllCompleted),
        Icon = IconValues.CheckCircle,
    };

    public DownloadsLeftMenuDesignViewModel()
    {
        var perGameItems = new[]
        {
            new LeftMenuItemDesignViewModel
            {
                Text = new StringComponent("Stardew Valley Downloads"),
                Icon = IconValues.FolderEditOutline,
            },
            new LeftMenuItemDesignViewModel
            {
                Text = new StringComponent("Cyberpunk 2077 Downloads"),
                Icon = IconValues.FolderEditOutline,
            },
            new LeftMenuItemDesignViewModel
            {
                Text = new StringComponent("Skyrim Downloads"),
                Icon = IconValues.FolderEditOutline,
            },
        };
        
        LeftMenuItemsPerGameDownloads = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(perGameItems));
    }
}