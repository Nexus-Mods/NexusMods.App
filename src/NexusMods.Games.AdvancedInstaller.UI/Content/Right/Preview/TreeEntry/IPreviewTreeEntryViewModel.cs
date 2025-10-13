using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

/// <summary>
///     Represents an individual tree entry in the Preview section.
///     It represents a single file or directory that will be installed, creating the final folder structure of the mod.
/// </summary>
/// <remarks>
///     Trees are build from top level LocationIds, all descendants are relative to the root GamePath.
///     If it happens that after a deletion, no files are deployed, the entire tree should be cleared.
/// </remarks>
public interface IPreviewTreeEntryViewModel : IViewModelInterface, IExpandableItem
{
    /// <summary>
    /// The GamePath of this entry.
    /// </summary>
    /// <remarks>
    /// The GamePath is relative to a top level LocationId and uniquely identifies an entry in the tree.
    /// </remarks>
    public GamePath GamePath { get; }

    /// <summary>
    /// The GamePath of the parent of this entry.
    /// </summary>
    public GamePath Parent { get; }

    /// <summary>
    /// The text displayed for this node.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether this entry is a directory.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// Whether this entry is a root node.
    /// There can be multiple root nodes, each representing a top level LocationId.
    /// </summary>
    public bool IsRoot { get; }

    /// <summary>
    /// Whether to show the (X) button on the UI of this entry.
    /// Always true for now.
    /// </summary>
    public bool IsRemovable { get; set; }

    /// <summary>
    /// Whether the entry was added by the user or if the folder was already present.
    /// This should always true for files.
    /// Should only be true for directories if it was added through a mapping of this archive.
    /// User created folders should not be marked as new.
    /// </summary>
    public bool IsNew { get; }

    /// <summary>
    /// Whether a mapped folder was merged into this previously existing folder.
    /// Previous folder could be from a previous mapping or created or already existing.
    /// </summary>
    public bool IsFolderMerged { get; set; }

    /// <summary>
    /// Whether the current folder has the same name as the parent folder,
    /// likely indicating an incorrect folder structure.
    /// </summary>
    public bool IsFolderDupe { get; }

    /// <summary>
    /// The mapped ModContent entry if this entry represents a direct file mapping.
    /// Ignored for directories.
    /// </summary>
    public Optional<IModContentTreeEntryViewModel> MappedEntry { get; set; }

    /// <summary>
    /// The collection of mapped ModContent directory entries, as multiple folders could be merged into this folder.
    /// Ignored for files.
    /// </summary>
    public ObservableCollection<IModContentTreeEntryViewModel> MappedEntries { get; }

    /// <summary>
    /// Command invoked when the (X) button is pressed on the UI.
    /// Will cause the removal of this mapping and all child mappings.
    /// Potentially will cause the removal of the entire tree if no mappings are left.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    /// <summary>
    /// Adds a direct mapping to this entry, either file or directory.
    /// </summary>
    /// <param name="entry">The ModContent entry being mapped.</param>
    public void AddMapping(IModContentTreeEntryViewModel entry);

    /// <summary>
    /// Removes a file mapping from this entry.
    /// Doesn't remove the entry from the tree.
    /// </summary>
    public void RemoveFileMapping();

    /// <summary>
    /// Removes a directory mapping from this entry.
    /// Doesn't remove mapping from child entries.
    /// Doesn't remove the entry from the tree.
    /// </summary>
    /// <param name="entry">The mapped mod content entry to remove (we could have multiple mappings).</param>
    public void RemoveDirectoryMapping(IModContentTreeEntryViewModel entry);

    /// <summary>
    /// The invalid GamePath used to represent the parent of a root entry.
    /// Necessary for DynamicData TransformToTree, we need a GamePath that is guaranteed not to represent another node.
    /// </summary>
    public static readonly GamePath RootParentGamePath = new(LocationId.Unknown, "");
}
