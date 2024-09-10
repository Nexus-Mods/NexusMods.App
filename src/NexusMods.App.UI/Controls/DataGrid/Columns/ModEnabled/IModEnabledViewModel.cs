using System.Reactive;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.DataGrid;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;

public interface IModEnabledViewModel : IColumnViewModel<LoadoutItemGroupId>
{
    public bool Enabled { get; }

    public ReactiveCommand<bool, Unit> ToggleEnabledCommand { get; }
}
