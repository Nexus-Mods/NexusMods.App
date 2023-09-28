using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Used as return values for games which are
/// deserialized (e.g. from data store) but not recognised by any add-in.
/// </summary>
public class UnknownGame : IGame
{
    private readonly GameDomain _slugName;
    private readonly Version _version;

    /// <summary/>
    /// <param name="domain">Domain for this game.</param>
    /// <param name="version">Version of the game [usually unknown].</param>
    public UnknownGame(GameDomain domain, Version version)
    {
        _slugName = domain;
        _version = version;
    }

    /// <inheritdoc />
    public string Name => $"Unknown Game ({_slugName})";

    /// <inheritdoc />
    public GameDomain Domain => _slugName;

    /// <inheritdoc />
    public IEnumerable<GameInstallation> Installations => new[]
    {
        new GameInstallation
        {
            Game = this,
            LocationsRegister = new GameLocationsRegister(new Dictionary<LocationId, AbsolutePath>()),
            Version = _version
        }
    };

    /// <inheritdoc />
    public void ResetInstallations() { }

    /// <inheritdoc />
    public IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        return Array.Empty<AModFile>();
    }

    /// <inheritdoc />
    public IEnumerable<IModInstaller> Installers => Array.Empty<IModInstaller>();

    /// <inheritdoc />
    public List<IModInstallDestination> InstallDestinations { get; } = new();

    /// <inheritdoc />
    public IStreamFactory Icon => throw new NotImplementedException("No icon provided for this game.");

    /// <inheritdoc />
    public IStreamFactory GameImage => throw new NotImplementedException("No game image provided for this game.");
}
