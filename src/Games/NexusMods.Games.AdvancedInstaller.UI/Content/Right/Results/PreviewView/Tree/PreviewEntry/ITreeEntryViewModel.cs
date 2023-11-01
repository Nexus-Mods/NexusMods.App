using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
/// <remarks>
///     We consider all entries delete-able, even those not added by the user in the results screen (such as
///     existing game folders that parents the selected mods).
///     This is such that the user can in one go delete all items as needed.
///     If it happens that after deletion, no files are deployed, the entire tree should be cleared.
/// </remarks>
public interface ITreeEntryViewModel : IViewModelInterface, IModContentBindingTarget
{
    /// <summary>
    ///     The full path of this node.
    /// </summary>
    public GamePath FullPath { get; init; }

    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    ObservableCollection<ITreeEntryViewModel> Children { get; }

    /// <summary>
    /// This is set to true when all child items are unliked from the tree, and this Location should be removed from the Preview
    /// </summary>
    bool ShouldRemove { get; }

    /// <summary>
    ///     The file name displayed for this node.
    /// </summary>
    string FileName { get; }

    /// <summary>
    ///     True if this is the root node. (Cannot be deleted)
    /// </summary>
    bool IsRoot { get; }

    /// <summary>
    ///     True if this is a directory, in which case all files from child of this will be mapped to given
    ///     target folder.
    /// </summary>
    bool IsDirectory { get; }

    /// <summary>
    ///     If this is true, the 'new' pill should be displayed in the UI.
    /// </summary>
    bool IsNew { get; }

    /// <summary>
    ///     If this is true the 'folder merged' pill should be displayed in the UI.
    /// </summary>
    bool IsFolderMerged { get; }

    /// <summary>
    ///     If this is true the 'dupe folder' pill should be displayed in the UI.
    /// </summary>
    bool IsFolderDuplicated { get; }

    /*
       Note:
       In the case of folder, the item can be merged or created from multiple sources, e.g. Multiple folders.
       when we unlink the folder/node, the user expects that all of the items under this node are unlinked.

       Therefore, we need to maintain a list of all items we can run an 'unlink' operation on, which can be either
       a file or a directory.
    */

    /// <summary>
    ///     The item with which this item is linked to.
    ///     If null, it's not been explicitly linked.
    /// </summary>
    IUnlinkableItem? LinkedItem { get; }
}
