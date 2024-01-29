using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory;

/// <summary>
/// Displays the category of a mod.
/// </summary>
public interface IModCategoryViewModel : IColumnViewModel<ModCursor>
{
    public string Category { get; }
}
