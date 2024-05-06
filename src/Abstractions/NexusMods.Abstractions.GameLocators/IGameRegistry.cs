using System.Collections.ObjectModel;
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
    /// Get an Observable of all installed games.
    /// </summary>
    public ReadOnlyObservableCollection<GameInstallation> InstalledGames { get; }
    
    /// <summary>
    /// Get a game installation by its id.
    /// </summary>
    public GameInstallation Get(EntityId id);
    
    /// <summary>
    /// Get the id for a game installation.
    /// </summary>
    public EntityId GetId(GameInstallation installation);
}
