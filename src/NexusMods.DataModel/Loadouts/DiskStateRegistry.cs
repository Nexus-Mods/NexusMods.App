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
    private readonly IDictionary<GameInstallation, IId> _lastAppliedRevisionDictionary = new Dictionary<GameInstallation, IId>();
    private readonly Subject<(GameInstallation gameInstallation, IId loadoutRevision)> _lastAppliedRevisionSubject = new();
    private readonly IConnection _connection;

    /// <inheritdoc />
    public IObservable<(GameInstallation gameInstallation, IId loadoutRevision)> LastAppliedRevisionObservable => _lastAppliedRevisionSubject;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public DiskStateRegistry(IConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Saves a disk state to the data store
    /// </summary>
    public async Task SaveState(GameInstallation installation, DiskStateTree diskState)
    {
        Debug.Assert(!diskState.LoadoutRevision.Equals(IdEmpty.Empty), "diskState.LoadoutRevision must be set");
        
        var db = _connection.Db;
        var tx = _connection.BeginTransaction();

        var previous = PreviousStateEntity(db, installation); 
        
        // If we have a previous state, update it
        if (previous is not null)
        {
            Attributes.DiskStateTree.LoadoutRevision.Add(tx, previous.Id, diskState.LoadoutRevision);
            Attributes.DiskStateTree.DiskState.Add(tx, previous.Id, diskState);
        }
        else
        {
            _ = new SavedDiskState(tx)
            {
                Game = installation.Game.Domain,
                Root = installation.LocationsRegister[LocationId.Game],
                LoadoutRevision = diskState.LoadoutRevision,
                DiskState = diskState,
            };
        }
        await tx.Commit();

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
        var result = PreviousStateEntity(db, gameInstallation);
        
        if (result is null) 
            return null;
        
        var state = result.DiskState;
        state.LoadoutRevision = result.LoadoutRevision;
        return state;
    }

    private static SavedDiskState? PreviousStateEntity(IDb db, GameInstallation gameInstallation)
    {
        return db
            .FindIndexed<Attributes.DiskStateTree.Root, AbsolutePath>(gameInstallation.LocationsRegister[LocationId.Game])
            .Select(db.Get<SavedDiskState>)
            .FirstOrDefault(state => state.Game == gameInstallation.Game.Domain);
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
