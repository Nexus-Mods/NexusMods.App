using System.Windows.Input;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

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
