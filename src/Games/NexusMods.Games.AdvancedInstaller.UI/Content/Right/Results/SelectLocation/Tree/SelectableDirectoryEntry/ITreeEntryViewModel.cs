using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
    new string DirectoryName { get; }

    public IAdvancedInstallerCoordinator Coordinator { get; }

    public ReactiveCommand<Unit,Unit> LinkCommand { get; }

    public void Link();
}
