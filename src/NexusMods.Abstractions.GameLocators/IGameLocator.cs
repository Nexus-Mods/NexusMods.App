namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// A service capable of finding the path to the game's installation directory.
/// </summary>
public interface IGameLocator
{
    /// <summary>
    /// For a given game, returns zero or more found paths to the game
    /// </summary>
    /// <param name="game">The game to find the location of.</param>
    /// <returns>Location of the game.</returns>
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game);
}
