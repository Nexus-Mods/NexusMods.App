using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators.Stores.GOG;

namespace NexusMods.Abstractions.Games.Stores.GOG;

/// <summary>
/// Metadata for games found that implement <see cref="IGogGame"/>.
/// </summary>
[PublicAPI]
public record GOGLocatorResultMetadata : IGameLocatorResultMetadata
{
    /// <summary>
    /// The specific ID of the found game.
    /// </summary>
    public required long Id { get; init; }
}
