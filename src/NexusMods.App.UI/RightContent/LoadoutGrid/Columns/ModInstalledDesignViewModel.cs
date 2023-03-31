using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModInstalledDesignViewModel : AViewModel<IModInstalledViewModel>, IModInstalledViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    public DateTime Installed { get; } =
        DateTime.UtcNow - TimeSpan.FromMinutes(42);
}
