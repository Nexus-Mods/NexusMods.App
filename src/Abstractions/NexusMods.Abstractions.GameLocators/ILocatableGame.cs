using NexusMods.Abstractions.NexusWebApi.Types.V2;
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
    /// Unique identifier for the game.
    /// This ID can be obtained from the V2 API.
    /// </summary>
    /// <remarks>
    ///     This can be obtained with a V2 call like:
    ///
    ///     ```
    ///     query Game {
    ///         game(domainName: "site") {
    ///             id
    ///         }
    ///     }
    ///
    ///     To https://api.nexusmods.com/v2/graphql
    ///     ```
    /// </remarks>
    public GameId GameId { get; }
}
