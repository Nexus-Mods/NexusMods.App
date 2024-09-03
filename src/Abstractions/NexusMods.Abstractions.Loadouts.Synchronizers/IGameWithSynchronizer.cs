using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A locatable game with a synchronizer.
/// </summary>
public interface IGameWithSynchronizer : ILocatableGame
{
        
    /// <summary>
    /// The synchronizer for this game.
    /// </summary>
    public ILoadoutSynchronizer Synchronizer { get; }
}
