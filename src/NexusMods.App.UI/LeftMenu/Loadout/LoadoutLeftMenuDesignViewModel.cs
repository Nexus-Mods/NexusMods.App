using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuDesignViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; } = new();
    public IApplyControlViewModel ApplyControlViewModel { get; } = new ApplyControlDesignViewModel();
    
    public INewLeftMenuItemViewModel LeftMenuItemLibrary { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.LibraryPageTitle,
        Icon = IconValues.LibraryOutline,
    };
    public INewLeftMenuItemViewModel LeftMenuItemLoadout { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.LoadoutView_Title_Installed_Mods,
        Icon = IconValues.Mods,
    };
    public INewLeftMenuItemViewModel LeftMenuItemHealthCheck { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics,
        Icon = IconValues.Cardiology,
    };

    public LoadoutLeftMenuDesignViewModel()
    {
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>([
                
                new IconViewModel
                {
                    Name = "My Collection",
                    Icon = IconValues.Collections,
                },

                new IconViewModel
                {
                    Name = "Stardew Valley Very Expanded",
                    Icon = IconValues.Collections,
                },
            ]
        );
    }
}
