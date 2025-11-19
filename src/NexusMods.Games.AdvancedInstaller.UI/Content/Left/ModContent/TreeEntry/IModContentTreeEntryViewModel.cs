using System.Reactive;
using DynamicData.Kernel;

using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

/// <summary>
///     Represents an individual node in the 'Mod Content' section.
///     A node can represent any file or directory within the mod being unpacked during advanced install.
/// </summary>
public interface IModContentTreeEntryViewModel : IViewModelInterface, IExpandableItem
{
    /// <summary>
    /// The path relative to the archive root.
    /// This also serves as a unique identifier for this entry in the tree.
    /// </summary>
    public RelativePath RelativePath { get; }

    /// <summary>
    /// The name of this file or folder.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Whether this is a directory.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// Whether this is the root node representing the archive root.
    /// Root is the "All mod files" node.
    /// </summary>
    public bool IsRoot { get; }

    /// <summary>
    /// The path of the parent directory, relative to the archive root.
    /// Uniquely identifies the parent directory.
    /// </summary>
    public RelativePath Parent { get; }

    /// <summary>
    /// Whether this is item is a direct child of the root node.
    /// </summary>
    public bool IsTopLevelChild { get; }

    /// <summary>
    /// The display name of the folder in which this item is being mapped to.
    /// Used to display the name of the folder in the Unlink button.
    /// </summary>
    public string MappingFolderName { get; set; }

    /// <summary>
    /// The installation path for this entry.
    /// Not present if this entry is not mapped.
    /// </summary>
    /// <remarks>
    /// The GamePath must be relative to a top level LocationId and uniquely identifies matching preview entry of this item.
    /// </remarks>
    public Optional<GamePath> Mapping { get; set; }

    /// <summary>
    /// The Selecting or Mapping (included) status of this entry.
    /// </summary>
    public ModContentTreeEntryStatus Status { get; set; }

    /// <summary>
    /// Command invoked when the user clicks the Install button on this entry.
    /// This selects the entry and all child entries for installation.
    /// The user is then prompted to select a location to install the entries to.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BeginSelectCommand { get; }

    /// <summary>
    /// Command invoked when the user clicks the (X Include) button.
    /// Removes this entry and child entries from install selection.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelSelectCommand { get; }

    /// <summary>
    /// Command invoked when the user clicks the (X location) or (X Included) button.
    /// Removes the mapping for this entry and child entries mapped with this entry.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    /// <summary>
    /// Sets the mapping information for this entry and changes the state.
    /// </summary>
    /// <param name="entry">Matching Preview tree entry</param>
    /// <param name="mappingFolderName">The name of the parent destination folder to display in the unlink button.</param>
    /// <param name="isExplicit">Whether this is an explicit mapping, or a child being mapped through a parent.</param>
    public void SetFileMapping(IPreviewTreeEntryViewModel entry, string mappingFolderName, bool isExplicit);

    /// <summary>
    /// Removes mapping information from this entry and changes the state.
    /// !Doesn't remove mapping information from child entries.
    /// </summary>
    public void RemoveMapping();

    /// <summary>
    /// The invalid relative path used for the root entry.
    /// Necessary for DynamicData TransformToTree, we need a RelativePath that is guaranteed not to represent another node.
    /// </summary>
    public static readonly RelativePath RootParentRelativePath = "*rootParent*";
}
