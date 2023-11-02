using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
public interface ITreeEntryViewModel : IViewModelInterface
{
    /// <summary>
    ///     Status of the node in question.
    /// </summary>
    public SelectableDirectoryNodeStatus Status { get; }

    /// <summary>
    /// The text of the create folder name input box.
    /// </summary>
    public string InputText { get; set; }

    /// <summary>
    ///     The full path associated with this node.
    /// </summary>
    public GamePath Path { get; }

    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    ObservableCollection<ITreeEntryViewModel> Children { get; }

    /// <summary>
    /// The Directory name displayed for this node.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The Directory name displayed for this node.
    /// </summary>
    string DirectoryName { get; }

    public IAdvancedInstallerCoordinator Coordinator { get; }

    public ReactiveCommand<Unit, Unit> LinkCommand { get; }

    public ReactiveCommand<Unit, Unit> EditCreateFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveCreatedFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> CancelCreateFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteCreatedFolderCommand { get; }

    public void Link();
}
