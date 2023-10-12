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
    ///     Returns the child of this target, i.e. child node in target's tree.
    /// </summary>
    /// <param name="name">The name of the child.</param>
    /// <param name="isDirectory">If this child does not exist, it will be created as directory if this is true.</param>
    /// <remarks>
    ///     If the child does not exist, it may be created.
    /// </remarks>
    IModContentBindingTarget GetOrCreateChild(string name, bool isDirectory);

    /// <summary>
    ///     Binds to the path represented by this target.
    /// </summary>
    /// <param name="unlinkable">You can use this item for unlinking.</param>
    /// <param name="previouslyExisted">This location previously existed (on FileSystem, or as result of another mod binding into that folder).</param>
    GamePath Bind(IUnlinkableItem unlinkable, bool previouslyExisted);

    /// <summary>
    ///     The Directory name target of the link.
    /// </summary>
    string DirectoryName { get; }
}
