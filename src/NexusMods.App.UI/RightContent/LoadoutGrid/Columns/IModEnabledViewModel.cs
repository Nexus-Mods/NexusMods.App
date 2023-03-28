using System.Windows.Input;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModEnabledViewModel : IColumnViewModel<ModId>
{
    public bool Enabled { get; }

    public ICommand ToggleEnabledCommand { get; }
}
