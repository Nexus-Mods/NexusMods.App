namespace NexusMods.App.UI.RightContent.LoadoutGrid;

/// <summary>
/// A way to identify the columns in the loadout grid in a way besides
/// a column type.
/// </summary>
public enum ColumnType
{
    Name,
    Version,
    Category,
    Installed,
    Enabled,
    
    // Download
    Game,
    Size,
    PauseResume
}
