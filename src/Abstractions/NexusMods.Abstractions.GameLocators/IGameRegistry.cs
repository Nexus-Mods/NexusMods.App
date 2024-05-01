using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Top level interface for a game registry, this will list all installed games for the system.
/// </summary>
public interface IGameRegistry
{
    /// <summary>
    /// Get all installed games.
    /// </summary>
    public IEnumerable<GameInstallation> AllInstalledGames { get; }

    /// <summary>
    /// Get a game installation by its id.
    /// </summary>
    public GameInstallation Get(EntityId id);
}
