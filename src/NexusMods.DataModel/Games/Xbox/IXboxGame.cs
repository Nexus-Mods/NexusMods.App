namespace NexusMods.DataModel.Games;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// <see cref="AGame"/> managed by Xbox Game Pass.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="XboxIds"/>
/// is applied.
/// </remarks>
public interface IXboxGame : IGame
{
    /// <summary>
    /// Returns one ore more Xbox IDs for the game.
    /// </summary>
    IEnumerable<string> XboxIds { get; }
}
