namespace NexusMods.Games.AdvancedInstaller.UI;

/// <summary>
///     Represents an item that can be unlinked from the deployment data.
/// </summary>
public interface IUnlinkableItem
{
    /// <summary>
    ///     Returns true if this unlinkable item represents a folder, else false.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    ///     Unlink the current node from the deployment data.
    /// </summary>
    public void Unlink(DeploymentData data);
}
