using NexusMods.Abstractions.Loadouts.Mods;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;

public class ModInstalledDesignViewModel : AViewModel<IModInstalledViewModel>, IModInstalledViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    public DateTime Installed { get; } =
        DateTime.UtcNow - TimeSpan.FromMinutes(42);

    [Reactive]
    public ModStatus Status { get; set; }
}
