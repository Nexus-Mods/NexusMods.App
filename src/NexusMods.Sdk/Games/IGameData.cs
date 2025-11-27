using System.Collections.Immutable;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Paths;
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
    /// Gets the store identifiers for the game.
    /// </summary>
    StoreIdentifiers StoreIdentifiers { get; }

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

    ImmutableDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult gameLocatorResult);

    /// <summary>
    /// Returns the primary (executable) file for the game.
    /// </summary>
    GamePath GetPrimaryFile(GameInstallation installation);

    /// <summary>
    /// Returns a game specific version.
    /// </summary>
    Optional<Version> GetLocalVersion(GameInstallation installation)
    {
        try
        {
            var primaryFile = GetPrimaryFile(installation);
            var fvi = installation.Locations.ToAbsolutePath(primaryFile).FileInfo.GetFileVersionInfo();
            return fvi.ProductVersion;
        }
        catch (Exception e)
        {
            return Optional<Version>.None;
        }
    }

    /// <summary>
    /// Gets the fallback directory for mods in collections that don't have matching installers.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/2553 for details.
    ///
    /// Also is used for bundled mods.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/2630 for details.
    /// </summary>
    Optional<GamePath> GetFallbackCollectionInstallDirectory(GameInstallation installation) => Optional<GamePath>.None;
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
