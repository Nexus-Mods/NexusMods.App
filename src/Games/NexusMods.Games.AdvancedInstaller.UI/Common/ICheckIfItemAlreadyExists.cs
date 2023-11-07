using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI;

/// <summary>
///     An interface that informs the node adding process whether an item has previously existed.
/// </summary>
public interface ICheckIfItemAlreadyExists
{
    /// <summary>
    ///     Returns true if the given path already exist
    /// </summary>
    /// <param name="path">The path to validate if it already exists in the game folder.</param>
    /// <returns>True if this path already exists, else false.</returns>
    bool AlreadyExists(GamePath path);
}

/// <summary>
///     A checker that always returns false.
/// </summary>
internal struct AlwaysFalseChecker : ICheckIfItemAlreadyExists
{
    public bool AlreadyExists(GamePath path) => true;
}
