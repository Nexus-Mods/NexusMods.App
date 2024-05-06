using System.Reactive;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;

/// <summary>
/// Displays the enabled state of a mod and a command to toggle it.
/// </summary>
public interface IModEnabledViewModel : IColumnViewModel<ModCursor>
{
    public bool Enabled { get; }
    public ModStatus Status { get; }

    public ReactiveCommand<bool, Unit> ToggleEnabledCommand { get; }
    
    public ReactiveCommand<Unit, Unit> DeleteModCommand { get; }
}
