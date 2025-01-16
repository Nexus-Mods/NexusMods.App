using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuDesignViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> LeftMenuCollectionItems { get; }
    public WorkspaceId WorkspaceId { get; } = new();
    public IApplyControlViewModel ApplyControlViewModel { get; } = new ApplyControlDesignViewModel();
    
    public ILeftMenuItemViewModel LeftMenuItemLibrary { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.LibraryPageTitle,
        Icon = IconValues.LibraryOutline,
    };
    public ILeftMenuItemViewModel LeftMenuItemLoadout { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.LoadoutView_Title_Installed_Mods,
        Icon = IconValues.Mods,
    };
    public ILeftMenuItemViewModel LeftMenuItemHealthCheck { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics,
        Icon = IconValues.Cardiology,
    };

    public LoadoutLeftMenuDesignViewModel()
    {
        LeftMenuCollectionItems = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>([
                
                new LeftMenuItemDesignViewModel()
                {
                    Text = "My Collection",
                    Icon = IconValues.CollectionsOutline,
                    IsToggleVisible = true,
                },
                
                new LeftMenuItemDesignViewModel()
                {
                    Text = "Stardew Valley Very Expanded",
                    Icon = IconValues.CollectionsOutline,
                    IsToggleVisible = true,
                },
            ]
        );
    }
}
