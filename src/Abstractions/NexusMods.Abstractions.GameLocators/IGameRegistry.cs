using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
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
    /// Try to get a game installation by its id.
    /// </summary>
    public bool TryGet(EntityId id, [NotNullWhen(true)] out GameInstallation? installation);
    
    /// <summary>
    /// Get the id for a game installation.
    /// </summary>
    public EntityId GetId(GameInstallation installation);
    
    /// <summary>
    /// Mostly used for testing and uncommon configurations. Manually added games won't
    /// show up in these caches, so calling this will requery all games from the database,
    /// and reset
    /// </summary>
    public Task Refresh();
}
