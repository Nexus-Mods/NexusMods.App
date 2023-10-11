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
