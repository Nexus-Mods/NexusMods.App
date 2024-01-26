using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModInstalled;

/// <summary>
/// Displays the installed date of a mod.
/// </summary>
public interface IModInstalledViewModel : IColumnViewModel<ModCursor>
{
    public DateTime Installed { get; }

    public ModStatus Status { get; }
}
