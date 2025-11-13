using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Sdk.Games;

[PublicAPI]
public interface IGameRegistry
{
    /// <summary>
    /// Locates all games installed on the system.
    /// </summary>
    ImmutableArray<GameInstallation> LocateGameInstallations();

    /// <summary>
    /// Clears the game installation cache.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Tries to get the game installation for a loadout.
    /// </summary>
    bool TryGetGameInstallation(Loadout.ReadOnly loadout, [NotNullWhen(true)] out GameInstallation? gameInstallation);

    /// <summary>
    /// Tries to get the game installation for a loadout and throws an exception if there isn't one.
    /// </summary>
    /// <exception cref="Exception">Thrown when there is no installation for the loadout</exception>
    GameInstallation ForceGetInstallation(Loadout.ReadOnly loadout)
    {
        if (!TryGetGameInstallation(loadout, out var installation)) throw new Exception($"Unable to find installation for Loadout {loadout.Name}");
        return installation;
    }

    /// <summary>
    /// Tries to get the installation metadata from the DB.
    /// </summary>
    bool TryGetMetadata(GameInstallation installation, out GameInstallMetadata.ReadOnly metadata);

    /// <summary>
    /// Tries to get the installation metadata from the DB and throws an exception if there isn't one.
    /// </summary>
    /// <exception cref="Exception">Thrown when there is no installation metadata in the DB</exception>
    GameInstallMetadata.ReadOnly ForceGetMetadata(GameInstallation installation)
    {
        if (!TryGetMetadata(installation, out var metadata)) throw new Exception($"Unable to find metadata for installation of {installation.Game.DisplayName} at {installation.LocatorResult.Path}");
        return metadata;
    }
}
