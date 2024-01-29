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

}
