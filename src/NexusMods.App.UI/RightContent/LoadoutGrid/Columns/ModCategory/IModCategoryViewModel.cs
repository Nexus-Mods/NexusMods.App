using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory;

/// <summary>
/// Displays the category of a mod.
/// </summary>
public interface IModCategoryViewModel : IColumnViewModel<ModCursor>
{
    public string Category { get; }
}
