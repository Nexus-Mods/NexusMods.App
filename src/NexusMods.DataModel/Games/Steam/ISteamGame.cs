namespace NexusMods.DataModel.Games;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// <see cref="AGame"/> managed by Steam.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="SteamIds"/>
/// is applied.
/// </remarks>
public interface ISteamGame : IGame
{
    /// <summary>
    /// Returns one or more steam app IDs for the game.
    /// </summary>
    IEnumerable<uint> SteamIds { get; }
}
