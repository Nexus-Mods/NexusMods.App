using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Used to store information about manually added games.
/// </summary>
[JsonName("ManuallyAddedGame")]
public record ManuallyAddedGame : Entity, IGameLocatorResultMetadata
{
    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.ManuallyAddedGame;

    /// <summary>
    /// The game domain this game install belongs to.
    /// </summary>
    public required GameDomain GameDomain { get; init; }

    /// <summary>
    /// The version of the game.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// The path to the game install.
    /// </summary>
    public required AbsolutePath Path { get; init; }
}
