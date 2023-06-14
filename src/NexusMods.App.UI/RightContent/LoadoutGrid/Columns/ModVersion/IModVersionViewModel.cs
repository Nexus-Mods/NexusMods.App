using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModVersion;

/// <summary>
/// Displays the version of a mod.
/// </summary>
public interface IModVersionViewModel : IColumnViewModel<ModCursor>
{
    public string Version { get; }
}
