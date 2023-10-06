using NexusMods.Games.AdvancedInstaller.UI.Content.Left;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

/// <summary>
///     Represents an individual node in the 'Suggested Entry' section.
/// </summary>
/// <remarks>
///     Using this at runtime isn't exactly ideal given how many items there may be, but given everything is virtualized,
///     things should hopefully be a-ok!
/// </remarks>
public interface ISuggestedEntryNode
{
    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     See <see cref="ModContentNode{TRelPath,TNodeValue}.Children"/>
    /// </remarks>
    ISuggestedEntryNode[] Children { get; }

    /// <summary>
    /// Returns true if the node is delete-able.
    /// When a node is user created, either by linking a file, or creating a folder, it is considered 'delete-able'.
    /// </summary>
    bool IsDeleteable { get; }
}
