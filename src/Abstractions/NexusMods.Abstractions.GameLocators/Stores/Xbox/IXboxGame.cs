namespace NexusMods.Abstractions.GameLocators.Stores.Xbox;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// games managed by Xbox Game Pass.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="XboxIds"/>
/// is applied.
/// </remarks>
public interface IXboxGame : ILocatableGame
{
    /// <summary>
    /// Returns one ore more Xbox IDs for the game.
    /// </summary>
    IEnumerable<string> XboxIds { get; }
}
