using System.Diagnostics;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public class DiskStateRegistry : IDiskStateRegistry
{
    private readonly ILogger<DiskStateRegistry> _logger;
    private readonly IDictionary<GameInstallation, IId> _lastAppliedRevisionDictionary = new Dictionary<GameInstallation, IId>();
    private readonly Subject<(GameInstallation gameInstallation, IId loadoutRevision)> _lastAppliedRevisionSubject = new();
    private readonly IConnection _connection;

    /// <inheritdoc />
    public IObservable<(GameInstallation gameInstallation, IId loadoutRevision)> LastAppliedRevisionObservable => _lastAppliedRevisionSubject;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public DiskStateRegistry(ILogger<DiskStateRegistry> logger, IConnection connection)
    {
        _logger = logger;
        _connection = connection;
    }

    /// <summary>
    /// Saves a disk state to the data store
    /// </summary>
    /// <returns></returns>
    public void SaveState(GameInstallation installation, DiskStateTree diskState)
    {
        Debug.Assert(!diskState.LoadoutRevision.Equals(IdEmpty.Empty), "diskState.LoadoutRevision must be set");
        
        var tx = _connection.BeginTransaction();
        _ = new SavedDiskState(tx)
        {
            Game = installation.Game.Domain,
            Root = installation.LocationsRegister[LocationId.Game],
            LoadoutRevision = diskState.LoadoutRevision,
            DiskState = diskState,
        };
        tx.Commit();

        _lastAppliedRevisionDictionary[installation] = diskState.LoadoutRevision;
        _lastAppliedRevisionSubject.OnNext((installation, diskState.LoadoutRevision));
    }
    
    /// <summary>
    /// Gets the disk state associated with a specific version of a loadout (if any)
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns></returns>
    public DiskStateTree? GetState(GameInstallation gameInstallation)
    {
        var db = _connection.Db;
        return db
            .FindIndexed<Attributes.DiskStateTree.Root, AbsolutePath>(gameInstallation.LocationsRegister[LocationId.Game])
            .Select(id => db.Get<SavedDiskState>(id))
            .Where(state => state.Game == gameInstallation.Game.Domain)
            .Select(state => state.DiskState)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public IId? GetLastAppliedLoadout(GameInstallation gameInstallation)
    {
        if (_lastAppliedRevisionDictionary.TryGetValue(gameInstallation, out var lastAppliedLoadout))
        {
            return lastAppliedLoadout;
        }

        var diskStateTree = GetState(gameInstallation);
        if (diskStateTree is null) return null;
        Debug.Assert(!diskStateTree.LoadoutRevision.Equals(IdEmpty.Empty), "diskState.LoadoutRevision must be set");

        _lastAppliedRevisionDictionary[gameInstallation] = diskStateTree.LoadoutRevision;
        return diskStateTree.LoadoutRevision;
    }
}
