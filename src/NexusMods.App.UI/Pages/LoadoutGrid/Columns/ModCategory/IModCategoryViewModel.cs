using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;

/// <summary>
/// Displays the category of a mod.
/// </summary>
public interface IModCategoryViewModel : IColumnViewModel<Mod.Model>
{
    public string Category { get; }
}
