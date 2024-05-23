using System.Collections.ObjectModel;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Top level interface for a game registry, this will list all installed games for the system.
/// </summary>
public interface IGameRegistry
{
    /// <summary>
    /// Get an Observable of all installed games.
    /// </summary>
    public ReadOnlyObservableCollection<GameInstallation> InstalledGames { get; }
    
    /// <summary>
    /// All the installations indexed by their ID.
    /// </summary>
    public IDictionary<EntityId, GameInstallation> Installations { get; }
}
