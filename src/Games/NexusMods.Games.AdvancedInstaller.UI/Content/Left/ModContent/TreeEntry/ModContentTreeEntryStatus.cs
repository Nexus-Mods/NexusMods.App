namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

/// <summary>
///     Represents the current status of the <see cref="NexusMods.Games.AdvancedInstaller.UI.ModContent.IModContentTreeEntryViewModel" />.
/// </summary>
public enum ModContentTreeEntryStatus : byte
{
    /// <summary>
    ///     Item is not selected, and available for selection.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     The item target is currently being selected/mapped.
    ///     This is used by the item which is currently being mapped into an install location.
    /// </summary>
    Selecting = 1,

    /// <summary>
    ///     A parent of this item (folder) is currently being selected/mapped.
    /// </summary>
    /// <remarks>
    ///     When this state is active, the UI shows 'include' for files, and 'include folder' for folders.
    /// </remarks>
    SelectingViaParent = 2,

    /// <summary>
    ///     Item is included, with explicit target location.
    /// </summary>
    /// <remarks>
    ///     When this state is active, the UI usually shows the name of the linked folder in the associated button.
    /// </remarks>
    IncludedExplicit = 3,

    /// <summary>
    ///     Item id included, because a parent (folder) of the item is included.
    ///     When the parent is unlinked, this node is also unlinked.
    /// </summary>
    /// <remarks>
    ///     This is used to indicate a parent of this item which which is a directory has status
    ///     <see cref="IncludedExplicit" />.
    /// </remarks>
    IncludedViaParent = 4,
}
