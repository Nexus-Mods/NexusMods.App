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

    public DownloadsLeftMenuDesignViewModel()
    {
        var perGameItems = new[]
        {
            new LeftMenuItemDesignViewModel
            {
                Text = new StringComponent(string.Format(Language.DownloadsLeftMenu_GameSpecificDownloads, "Stardew Valley")),
                Icon = IconValues.FolderEditOutline,
            },
            new LeftMenuItemDesignViewModel
            {
                Text = new StringComponent(string.Format(Language.DownloadsLeftMenu_GameSpecificDownloads, "Cyberpunk 2077")),
                Icon = IconValues.FolderEditOutline,
            },
            new LeftMenuItemDesignViewModel
            {
                Text = new StringComponent(string.Format(Language.DownloadsLeftMenu_GameSpecificDownloads, "Skyrim")),
                Icon = IconValues.FolderEditOutline,
            },
        };
        
        LeftMenuItemsPerGameDownloads = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(perGameItems));
    }
}