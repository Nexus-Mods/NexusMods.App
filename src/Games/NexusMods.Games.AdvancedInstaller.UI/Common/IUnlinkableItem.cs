namespace NexusMods.Games.AdvancedInstaller.UI;

/// <summary>
///     Represents an item that can be unlinked from the deployment data.
/// </summary>
public interface IUnlinkableItem
{
    /// <summary>
    ///     Removes itself and all of its children recursively from the deployment data.
    /// </summary>
    /// <param name="data">The deployment data.</param>
    /// <param name="isCalledFromDoubleLinkedItem">
    ///     If this is true, the <see cref="Unlink"/> method was called from another <see cref="IUnlinkableItem"/> which
    ///     is being unlinked (it's doubly linked to this item).
    ///
    ///     If this is true, do not call `unlink` on the other item.
    /// </param>
    public void Unlink(bool isCalledFromDoubleLinkedItem);
}
