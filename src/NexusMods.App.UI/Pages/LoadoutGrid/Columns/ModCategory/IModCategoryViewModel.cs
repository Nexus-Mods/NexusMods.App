using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;

/// <summary>
/// Displays the category of a mod.
/// </summary>
public interface IModCategoryViewModel : IColumnViewModel<ModId>
{
    public string Category { get; }
}
