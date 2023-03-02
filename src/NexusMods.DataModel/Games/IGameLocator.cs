namespace NexusMods.DataModel.Games;

/// <summary>
/// A service capable of finding the path to the game's installation directory.
/// </summary>
public interface IGameLocator
{
    /// <summary>
    /// For a given game, returns zero or more found paths to the game
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    public IEnumerable<GameLocatorResult> Find(IGame game);
}
