using NexusMods.Abstractions.Games.DTO;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModName;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IModNameViewModel : IColumnViewModel<ModCursor>
{
    public string Name { get; }
}
