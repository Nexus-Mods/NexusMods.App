using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModInstalledDesignViewModel : AViewModel<IModInstalledViewModel>, IModInstalledViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    public DateTime Installed { get; } =
        DateTime.UtcNow - TimeSpan.FromMinutes(42);

    [Reactive]
    public ModStatus Status { get; set; }
}
