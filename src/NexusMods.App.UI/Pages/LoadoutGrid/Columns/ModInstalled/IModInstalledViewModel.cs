using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;

/// <summary>
/// Displays the installed date of a mod.
/// </summary>
public interface IModInstalledViewModel : IColumnViewModel<ModId>, ICellViewModel<DateTime>;
