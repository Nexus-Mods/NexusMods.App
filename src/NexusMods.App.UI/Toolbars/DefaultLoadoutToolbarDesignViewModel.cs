using System.ComponentModel;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Toolbars;

public class DefaultLoadoutToolbarDesignViewModel : AViewModel<IDefaultLoadoutToolbarViewModel>, IDefaultLoadoutToolbarViewModel
{
    [Reactive]
    public string Caption { get; set; } = "Some Game Name Goes Here";
    public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    public async Task StartManualModInstall(string path)
    {
        Thread.Sleep(1000);
    }
}
