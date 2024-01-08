using JetBrains.Annotations;

namespace NexusMods.DataModel.Games;

/// <summary>
/// <see cref="IGameLocatorResultMetadata"/> implementation for nothing.
/// </summary>
[PublicAPI]
public record UnknownLocatorResultMetadata : IGameLocatorResultMetadata;
