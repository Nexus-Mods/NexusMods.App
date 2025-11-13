using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Games;

/// <summary>
/// Represents a game locator.
/// </summary>
[PublicAPI]
public interface IGameLocator
{
    /// <summary>
    /// Locates all registered games.
    /// </summary>
    IEnumerable<GameLocatorResult> Locate();

    /// <summary>
    /// Tries to locate a single game.
    /// </summary>
    bool TryLocate(IGameData game, [NotNullWhen(true)] out GameLocatorResult? locatorResult)
    {
        return Locate().TryGetFirst(result => result.Game.GameId == game.GameId, out locatorResult);
    }
}
