using System.Windows.Input;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModEnabled;

/// <summary>
/// Displays the enabled state of a mod and a command to toggle it.
/// </summary>
public interface IModEnabledViewModel : IColumnViewModel<ModCursor>
{
    public bool Enabled { get; }
    public ModStatus Status { get; }

    public ICommand ToggleEnabledCommand { get; }
    public ICommand DeleteModCommand { get; }
}
