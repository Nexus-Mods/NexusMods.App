using System.Collections.ObjectModel;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuDesignViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; } = new();
    public IApplyControlViewModel ApplyControlViewModel { get; } = new ApplyControlDesignViewModel();
    
    public LoadoutLeftMenuDesignViewModel()
    {
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>([
                new IconViewModel
                {
                    Name = "My Mods",
                    Icon = IconValues.Collections,
                    Activate = ReactiveCommand.Create(() => { }),
                },

                new IconViewModel
                {
                    Name = "Diagnostics",
                    Icon = IconValues.MonitorDiagnostics,
                    Activate = ReactiveCommand.Create(() => { }),
                },
            ]
        );
    }
}
