using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Sdk.IO;

namespace NexusMods.Sdk.Games;

[PublicAPI]
public interface IGameData
{
    /// <summary>
    /// Gets the unique identifier for the game.
    /// </summary>
    GameId GameId { get; }

    /// <summary>
    /// Gets the display name of the game.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the ID of the game on Nexus Mods.
    /// </summary>
    Optional<NexusModsApi.NexusModsGameId> NexusModsGameId { get; }

    /// <summary>
    /// Gets the stream factory for the square icon image.
    /// </summary>
    IStreamFactory IconImage { get; }

    /// <summary>
    /// Gets the stream factory for the horizontal tile image.
    /// </summary>
    IStreamFactory TileImage { get; }
}

[PublicAPI]
public interface IGameData<TSelf> : IGameData
    where TSelf : IGameData, IGameData<TSelf>
{
    /// <inheritdoc cref="IGameData.GameId"/>
    new static abstract GameId GameId { get; }
    GameId IGameData.GameId => TSelf.GameId;

    /// <inheritdoc cref="IGameData.DisplayName"/>
    new static abstract string DisplayName { get; }
    string IGameData.DisplayName => TSelf.DisplayName;

    /// <inheritdoc cref="IGameData.NexusModsGameId"/>
    new static abstract Optional<NexusModsApi.NexusModsGameId> NexusModsGameId { get; }
    Optional<NexusModsApi.NexusModsGameId> IGameData.NexusModsGameId => TSelf.NexusModsGameId;
}
