using System.Diagnostics;
using System.Reactive.Subjects;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Attributes;
using NexusMods.MnemonicDB.Abstractions;

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

    /// <inheritdoc />
    public async Task SaveState(GameInstallation installation, DiskStateTree diskState)
    {
        Debug.Assert(!diskState.LoadoutRevision.Equals(IdEmpty.Empty), "diskState.LoadoutRevision must be set");
        
        var db = _connection.Db;
        var tx = _connection.BeginTransaction();

        var previous = PreviousStateEntity(db, installation); 
        
        // If we have a previous state, update it
        if (previous is not null)
        {
            tx.Add(previous.Id, DiskState.LoadoutRevision, diskState.LoadoutRevision);
            tx.Add(previous.Id, DiskState.State, diskState);
        }
        else
        {
            _ = new DiskState.Model(tx)
            {
                Game = installation.Game.Domain,
                Root = installation.LocationsRegister[LocationId.Game].ToString(),
                LoadoutRevision = diskState.LoadoutRevision,
                DiskState = diskState,
            };
        }
        await tx.Commit();

        _lastAppliedRevisionDictionary[installation] = diskState.LoadoutRevision;
        _lastAppliedRevisionSubject.OnNext((installation, diskState.LoadoutRevision));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task SaveInitialState(GameInstallation installation, DiskStateTree diskState)
    {
        // Initial state is identified by an empty LoadoutRevision

        // I looked for a 'clean' way to do this, and I found that it's
        // not possible for us to create an `IdEmpty.Empty` with actual
        // IDs, so the loadoutrevision of Empty for a game is reserved for
        // the very first initial state.
        
        // Actual loadouts at time of writing use Id64, composed of category and hash.
        // As is standard for our data model; there's no special casing there.

        // - Sewer
        
        var tx = _connection.BeginTransaction();

        _ = new DiskState.Model(tx)
        {
            Game = installation.Game.Domain,
            Root = installation.LocationsRegister[LocationId.Game].ToString(),
            LoadoutRevision = IdEmpty.Empty, 
            DiskState = diskState,
        };

        await tx.Commit();
    }

    /// <inheritdoc />
    public DiskStateTree? GetInitialState(GameInstallation installation)
    {
        // Initial state is identified by an empty LoadoutRevision
        var db = _connection.Db;
        var result = db
            .FindIndexed(installation.LocationsRegister[LocationId.Game].ToString(), DiskState.Root)
            .Select(db.Get<DiskState.Model>)
            .FirstOrDefault(state => state.Game == installation.Game.Domain && state.LoadoutRevision.Equals(IdEmpty.Empty));

        return result?.DiskState;
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
    
    private static DiskState.Model? PreviousStateEntity(IDb db, GameInstallation gameInstallation)
    {
        return db
            .FindIndexed(gameInstallation.LocationsRegister[LocationId.Game].ToString(), DiskState.Root)
            .Select(db.Get<DiskState.Model>)
            .FirstOrDefault(state => state.Game == gameInstallation.Game.Domain);
    }
}
