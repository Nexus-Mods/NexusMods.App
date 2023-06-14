using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModName;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IModNameViewModel : IColumnViewModel<ModCursor>
{
    public string Name { get; }
}
