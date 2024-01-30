using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Several extensions for game related classes
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Returns the game of a game installation, casted into an IGame for convenience.
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns></returns>
    public static IGame GetGame(this GameInstallation gameInstallation) => (IGame)gameInstallation.Game;
}
