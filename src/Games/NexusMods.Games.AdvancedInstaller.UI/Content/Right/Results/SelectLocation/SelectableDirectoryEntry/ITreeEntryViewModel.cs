using System.Collections.ObjectModel;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
public interface ITreeEntryViewModel
{
    /// <summary>
    ///     Status of the node in question.
    /// </summary>
    [Reactive]
    public SelectableDirectoryNodeStatus Status { get; }

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
}
