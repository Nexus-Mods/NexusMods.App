namespace NexusMods.Abstractions.GameLocators.Stores.Steam;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// games managed by Steam.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="SteamIds"/>
/// is applied.
/// </remarks>
[Obsolete("Use IGameData.StoreIdentifiers instead")]
public interface ISteamGame : ILocatableGame
{
    /// <summary>
    /// Returns one or more steam app IDs for the game.
    /// </summary>
    IEnumerable<uint> SteamIds { get; }
}
