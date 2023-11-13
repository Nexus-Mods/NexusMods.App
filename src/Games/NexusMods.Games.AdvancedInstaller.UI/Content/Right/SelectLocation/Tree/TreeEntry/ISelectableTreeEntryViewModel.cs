using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

/// <summary>
/// Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
public interface ISelectableTreeEntryViewModel : IViewModelInterface
{
    public GamePath GamePath { get; }

    public string DisplayName { get; }

    public string InputText { get; set; }

    public bool IsRoot { get; }

    public GamePath Parent { get; }

    public SelectableDirectoryNodeStatus Status { get; set; }

    public bool HasActiveLink { get; set; }

    public ReactiveCommand<Unit, Unit> CreateMappingCommand { get; }

    public ReactiveCommand<Unit, Unit> EditCreateFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveCreatedFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> CancelCreateFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteCreatedFolderCommand { get; }
}

/// <summary>
/// Represents the current status of the <see cref="SelectableTreeEntryViewModel" />.
/// </summary>
public enum SelectableDirectoryNodeStatus
{
    /// <summary>
    /// Regular selectable directory node. Generated from game locations and Loadout folders.
    /// </summary>
    Regular,

    /// <summary>
    /// Selectable directory node, volatile, shows folder structure from mappings and removed when mappings are removed.
    /// </summary>
    RegularFromMapping,

    /// <summary>
    /// Special "Create new folder" entry node.
    /// </summary>
    Create,

    /// <summary>
    /// Create node after button was pressed, user can input the name of the new folder.
    /// </summary>
    Editing,

    /// <summary>
    /// A new node created with "Create new folder" button
    /// </summary>
    Created,
}
