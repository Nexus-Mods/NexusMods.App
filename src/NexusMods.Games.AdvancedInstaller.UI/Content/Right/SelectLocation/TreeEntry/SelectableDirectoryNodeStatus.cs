namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

/// <summary>
/// Represents the current status of the <see cref="SelectableTreeEntryViewModel" />.
/// </summary>
public enum SelectableDirectoryNodeStatus : byte
{
    /// <summary>
    /// Regular selectable directory node. Generated from game locations and Loadout folders.
    /// </summary>
    Regular = 0,

    /// <summary>
    /// Selectable directory node, volatile, shows folder structure from mappings and removed when mappings are removed.
    /// </summary>
    RegularFromMapping = 1,

    /// <summary>
    /// Special "Create new folder" entry node.
    /// </summary>
    Create = 2,

    /// <summary>
    /// Create node after button was pressed, user can input the name of the new folder.
    /// </summary>
    Editing = 3,

    /// <summary>
    /// A new node created with "Create new folder" button
    /// </summary>
    Created = 4,
}
