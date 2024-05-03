using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IModNameViewModel : IColumnViewModel<ModId>, ICellViewModel<string>;
