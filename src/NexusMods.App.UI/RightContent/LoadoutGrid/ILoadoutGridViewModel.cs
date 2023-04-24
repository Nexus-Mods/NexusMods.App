using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

/// <summary>
/// View model for the loadout grid.
/// </summary>
public interface ILoadoutGridViewModel : IRightContentViewModel
{
    public ReadOnlyObservableCollection<ModCursor> Mods { get; }
    public LoadoutId LoadoutId { get; set; }
    public string LoadoutName { get; }

    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns { get; }

    public Task AddMod(string path);

}
