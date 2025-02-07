using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.StandardGameLocators.Unknown;

/// <summary>
/// <see cref="IGameLocatorResultMetadata"/> implementation for nothing.
/// </summary>
[PublicAPI]
public record UnknownLocatorResultMetadata : IGameLocatorResultMetadata
{
    public IEnumerable<string> ToLocatorIds() => ["StubbedGameState.zip"];
}
