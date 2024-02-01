using NexusMods.Abstractions.Games.DTO;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Base interface for a game which can be located by a <see cref="IGameLocator"/>.
/// </summary>
public interface ILocatableGame
{
    /// <summary>
    /// Human readable name of the game.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Machine friendly name for the game, should be devoid of special characters
    /// that may conflict with URLs or file paths.
    /// </summary>
    /// <remarks>
    ///    Usually we match these with NexusMods' URLs.
    /// </remarks>
    public GameDomain Domain { get; }
}
