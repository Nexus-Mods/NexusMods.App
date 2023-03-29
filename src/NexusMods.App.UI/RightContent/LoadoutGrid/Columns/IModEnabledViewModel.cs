using System.Windows.Input;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModEnabledViewModel : IColumnViewModel<ModCursor>
{
    public bool Enabled { get; }

    public ICommand ToggleEnabledCommand { get; }
}
