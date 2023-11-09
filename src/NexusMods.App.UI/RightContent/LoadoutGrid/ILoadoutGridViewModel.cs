using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.DataGrid;
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

    public ReadOnlyObservableCollection<IDataGridColumnFactory<LoadoutColumn>> Columns { get; }

    /// <summary>
    /// Add a mod to the loadout using the standard installer.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task AddMod(string path);

    /// <summary>
    /// Add a mod to the loadout using the advanced installer.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task AddModAdvanced(string path);

    /// <summary>
    /// Delete the mods from the loadout.
    /// </summary>
    /// <param name="modsToDelete"></param>
    /// <param name="commitMessage"></param>
    /// <returns></returns>
    public Task DeleteMods(IEnumerable<ModId> modsToDelete, string commitMessage);

}
