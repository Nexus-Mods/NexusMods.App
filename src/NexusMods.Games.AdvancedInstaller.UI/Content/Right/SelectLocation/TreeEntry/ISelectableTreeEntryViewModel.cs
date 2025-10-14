using System.Reactive;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Paths;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

/// <summary>
/// Represents an individual entry in the 'All Folders' tree when selecting an install location.
/// This always represents a directory where mod files can be mapped to.
/// </summary>
public interface ISelectableTreeEntryViewModel : IViewModelInterface, IExpandableItem
{
    /// <summary>
    /// The GamePath relative to a top level location of this directory.
    /// </summary>
    public GamePath GamePath { get; }

    /// <summary>
    /// Either the directory name or the location id for root nodes.
    /// Used in the UI.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The user input text of the create folder name input box.
    /// </summary>
    public string InputText { get; set; }

    /// <summary>
    /// Whether the entry is a root node.
    /// </summary>
    public bool IsRoot { get; }

    /// <summary>
    /// The GamePath of the parent directory.
    /// RootParentGamePath for root nodes.
    /// </summary>
    public GamePath Parent { get; }

    /// <summary>
    /// The status representing the type of entry, mostly for CreateFolder related entries.
    /// </summary>
    public SelectableDirectoryNodeStatus Status { get; set; }

    /// <summary>
    /// Command invoked when the user clicks the "Select" button.
    /// Will cause all the selected files to be mapped under this directory.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateMappingCommand { get; }

    /// <summary>
    /// Command invoked when the user clicks the "Create new folder" button.
    /// Will show an input box for the user to input the name of the new folder.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditCreateFolderCommand { get; }

    /// <summary>
    /// Command invoked when the user clicks the "Save" button while editing the name of the new folder.
    /// Will create a new entry with the input name as the directory name.
    /// No op if the input is empty, or directory name already exists.
    /// Should only be enabled if input could be valid.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCreatedFolderCommand { get; }

    /// <summary>
    /// Command invoked when the user clicks the "Cancel" button while editing the name of the new folder.
    /// Will return to show the Create new folder button.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCreateFolderCommand { get; }

    /// <summary>
    /// Command invoked when the user clicks the "Delete" button while editing the name of the new folder.
    /// Will remove the entry from the tree.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteCreatedFolderCommand { get; }

    /// <summary>
    /// Returns the name of the directory after removing invalid characters and trimming.
    /// </summary>
    /// <returns></returns>
    public RelativePath GetSanitizedInput();

    /// <summary>
    /// Invalid GamePath for parents of root nodes. Used to avoid matching another node by accident.
    /// Required for DynamicData.
    /// </summary>
    public static readonly GamePath RootParentGamePath = new(LocationId.Unknown, string.Empty);

    /// <summary>
    /// Invalid RelativePath for the CreateFolder nodes, necessary since RelativePath is used as the key in DynamicData.
    /// </summary>
    public static readonly RelativePath CreateFolderEntryName = "*CreateFolder*";
}
