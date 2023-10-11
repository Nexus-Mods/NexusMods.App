using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

/// <summary>
///     Represents an individual node in the 'Suggested Entry' section. (located above the 'all folders' section)
///     This displays folders that the user can select to deploy a mod into.
/// </summary>
public interface ISuggestedEntryNode
{
    /// <summary>
    ///     The Directory name displayed for this node.
    /// </summary>
    string DirectoryName { get; }

    /// <summary>
    ///     Short description for this item.
    /// </summary>
    string Description { get; }
}

/// <summary>
///     An interface for an item which can be bound to a source file/folder within a game mod archive.
/// </summary>
/// <remarks>
///     This is part of <see cref="ISuggestedEntryNode" /> but separated for easier testing.
/// </remarks>
public interface IModContentBindingTarget
{
    /// <summary>
    ///     Binds to the path represented by this target.
    /// </summary>
    /// <param name="unlinkable">You can use this item for unlinking.</param>
    GamePath Bind(IUnlinkableItem unlinkable);

    /// <summary>
    ///     The Directory name target of the link.
    /// </summary>
    string DirectoryName { get; }
}
