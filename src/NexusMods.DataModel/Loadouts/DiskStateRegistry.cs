using System.Diagnostics;
using System.Reactive.Subjects;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.DataModel.Attributes;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public class DiskStateRegistry : IDiskStateRegistry
{
    private readonly Dictionary<GameInstallation, LoadoutWithTxId> _lastAppliedRevisionDictionary = new();
    private readonly Subject<(GameInstallation gameInstallation, LoadoutWithTxId)> _lastAppliedRevisionSubject = new();
    private readonly IConnection _connection;

    /// <inheritdoc />
    public IObservable<(GameInstallation Install, LoadoutWithTxId LoadoutRevisionId)> LastAppliedRevisionObservable => _lastAppliedRevisionSubject;

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
        Debug.Assert(diskState.LoadoutId != EntityId.From(0), "diskState.LoadoutId must be set");
        Debug.Assert(diskState.TxId != TxId.From(0), "diskState.LoadoutId must be set");
        
        var db = _connection.Db;
        var tx = _connection.BeginTransaction();

        var previous = PreviousStateEntity(db, installation); 
        
        // If we have a previous state, update it
        if (previous is not null)
        {
            tx.Add(previous.Id, DiskState.Loadout, diskState.LoadoutId);
            tx.Add(previous.Id, DiskState.TxId, EntityId.From(diskState.TxId.Value));
            tx.Add(previous.Id, DiskState.State, diskState);
        }
        else
        {
            _ = new DiskState.Model(tx)
            {
                Game = installation.Game.Domain,
                Root = installation.LocationsRegister[LocationId.Game].ToString(),
                LoadoutId = diskState.LoadoutId,
                TxId = diskState.TxId,
                DiskState = diskState,
            };
        }
        await tx.Commit();


        var id = new LoadoutWithTxId { Id = LoadoutId.From(diskState.LoadoutId), Tx = diskState.TxId };
        _lastAppliedRevisionDictionary[installation] = id;
        _lastAppliedRevisionSubject.OnNext((installation, id));
    }

    /// <inheritdoc />
    public DiskStateTree? GetState(GameInstallation gameInstallation)
    {
        var db = _connection.Db;
        var result = PreviousStateEntity(db, gameInstallation);
        
        if (result is null) 
            return null;
        
        var state = result.DiskState;
        state.LoadoutId = result.LoadoutId;
        state.TxId = result.TxId;
        return state;
    }

    /// <inheritdoc />
    public async Task SaveInitialState(GameInstallation installation, DiskStateTree diskState)
    {
        var tx = _connection.BeginTransaction();
        var domain = installation.Game.Domain;
        _ = new InitialDiskState.Model(tx)
        {
            Game = domain,
            Root = installation.LocationsRegister[LocationId.Game].ToString(),
            DiskState = diskState,
        };

        await tx.Commit();
    }

    /// <inheritdoc />
    public DiskStateTree? GetInitialState(GameInstallation installation)
    {
        var db = _connection.Db;

        return db.FindIndexed(installation.LocationsRegister[LocationId.Game].ToString(), InitialDiskState.Root)
            .Select(x => db.Get<InitialDiskState.Model>(x).DiskState)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task ClearInitialState(GameInstallation installation)
    {
        var db = _connection.Db;
        var initialDiskState = db.FindIndexed(installation.LocationsRegister[LocationId.Game].ToString(), InitialDiskState.Root)
            .Select(x => db.Get<InitialDiskState.Model>(x))
            .FirstOrDefault();
        
        if (initialDiskState == null)
            return;

        var tx = _connection.BeginTransaction();
        initialDiskState.Tx = tx;
        initialDiskState.AddRetractToCurrentTx();
        await tx.Commit();
    }

    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out LoadoutWithTxId id)
    {
        if (_lastAppliedRevisionDictionary.TryGetValue(gameInstallation, out var lastAppliedLoadout))
        {
            using var conn = _connection.AsOf(lastAppliedLoadout.Tx);
            var model = conn.Get<Loadout.Model>(lastAppliedLoadout.Id.Value);
            id = lastAppliedLoadout;
            return model.LoadoutKind != LoadoutKind.Deleted;
        }

        var diskStateTree = GetState(gameInstallation);
        if (diskStateTree is null)
        {
            id = new LoadoutWithTxId()
            {
                Id = LoadoutId.From(EntityId.MinValue),
                Tx = TxId.MinValue
            };
            return false;
        }
        
        id = new LoadoutWithTxId { Id = LoadoutId.From(diskStateTree.LoadoutId), Tx = diskStateTree.TxId };
        _lastAppliedRevisionDictionary[gameInstallation] = id;
        return true;
    }
    
    private static DiskState.Model? PreviousStateEntity(IDb db, GameInstallation gameInstallation)
    {
        return db
            .FindIndexed(gameInstallation.LocationsRegister[LocationId.Game].ToString(), DiskState.Root)
            .Select(db.Get<DiskState.Model>)
            .FirstOrDefault(state => state.Game == gameInstallation.Game.Domain);
    }
}
