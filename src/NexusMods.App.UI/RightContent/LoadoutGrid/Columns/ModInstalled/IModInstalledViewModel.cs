using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModInstalled;

/// <summary>
/// Displays the installed date of a mod.
/// </summary>
public interface IModInstalledViewModel : IColumnViewModel<ModCursor>
{
    public DateTime Installed { get; }

    public ModStatus Status { get; }
}
