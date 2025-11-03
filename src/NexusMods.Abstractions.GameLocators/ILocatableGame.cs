using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Base interface for a game which can be located by a <see cref="IGameLocator"/>.
/// </summary>
public interface ILocatableGame
{
    /// <summary>
    /// Human readable name of the game.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Nexus Mods Game Id.
    /// </summary>
    GameId NexusModsGameId { get; }
}
