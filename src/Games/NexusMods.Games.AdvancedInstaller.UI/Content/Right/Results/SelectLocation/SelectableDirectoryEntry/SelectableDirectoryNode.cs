using NexusMods.Games.AdvancedInstaller.UI.Content.Left;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
public interface ISelectableDirectoryNode
{
    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     See <see cref="ModContentNode{TNodeValue}.Children"/>
    /// </remarks>
    ISelectableDirectoryNode[] Children { get; }

    /// <summary>
    /// The Directory name displayed for this node.
    /// </summary>
    string DirectoryName { get; }

    /// <summary>
    /// Returns true if the node is delete-able.
    /// When a node is user created, either by linking a file, or creating a folder, it is considered 'delete-able'.
    /// </summary>
    bool IsDeleteable { get; }
}
