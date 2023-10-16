using System.Collections.ObjectModel;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
public interface ISelectableDirectoryNode
{
    /// <summary>
    ///     Status of the node in question.
    /// </summary>
    [Reactive]
    public SelectableDirectoryNodeStatus Status { get; }

    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     See <see cref="ModContentNode{TNodeValue}.Children"/>
    /// </remarks>
    ObservableCollection<ITreeEntryViewModel> Children { get; }

    /// <summary>
    /// The Directory name displayed for this node.
    /// </summary>
    string DirectoryName { get; }
}


public class SelectableDirectoryNode : ReactiveObject, ISelectableDirectoryNode
{
    [Reactive] public SelectableDirectoryNodeStatus Status { get; private set;}
    public ObservableCollection<ITreeEntryViewModel> Children { get; init; } = new();
    public string DirectoryName { get; } = string.Empty;
}


/// <summary>
///     Represents the current status of the <see cref="SelectableDirectoryNode" />.
/// </summary>
public enum SelectableDirectoryNodeStatus
{
    /// <summary>
    /// Regular selectable directory node.
    /// </summary>
    Regular,

    /// <summary>
    /// Special "Create new folder" entry node.
    /// </summary>
    Create,

    /// <summary>
    /// Create node after button was pressed, user can input the name of the new folder.
    /// </summary>
    Editing,

    /// <summary>
    /// A new node created with "Create new folder button
    /// </summary>
    Created,
}
