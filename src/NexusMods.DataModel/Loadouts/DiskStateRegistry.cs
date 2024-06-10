using System.Diagnostics;
using System.Reactive.Subjects;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.DataModel.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

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
            tx.Add(previous.Value.Id, DiskState.Loadout, diskState.LoadoutId);
            tx.Add(previous.Value.Id, DiskState.TxId, diskState.TxId);
            tx.Add(previous.Value.Id, DiskState.State, diskState);
        }
        else
        {
            _ = new DiskState.New(tx)
            {
                Game = installation.Game.Domain,
                Root = installation.LocationsRegister[LocationId.Game].ToString(),
                LoadoutId = diskState.LoadoutId,
                TxId = diskState.TxId,
                State = diskState,
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
        
        var state = result.Value.State;
        state.LoadoutId = result.Value.LoadoutId;
        state.TxId = result.Value.TxId;
        return state;
    }

    /// <inheritdoc />
    public async Task SaveInitialState(GameInstallation installation, DiskStateTree diskState)
    {
        var tx = _connection.BeginTransaction();
        var domain = installation.Game.Domain;
        _ = new InitialDiskState.New(tx)
        {
            Game = domain,
            Root = installation.LocationsRegister[LocationId.Game].ToString(),
            State = diskState,
        };

        await tx.Commit();
    }

    /// <inheritdoc />
    public DiskStateTree? GetInitialState(GameInstallation installation)
    {
        var db = _connection.Db;

        return InitialDiskState.FindByRoot(db, installation.LocationsRegister[LocationId.Game].ToString())
            .Select(x => x.State)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task ClearInitialState(GameInstallation installation)
    {
        var db = _connection.Db;
        var initialDiskState = InitialDiskState.FindByRoot(db, installation.LocationsRegister[LocationId.Game].ToString())
            .FirstOrDefault();
        
        if (!initialDiskState.IsValid())
            return;

        var tx = _connection.BeginTransaction();
        tx.Delete(initialDiskState.Id, false);
        await tx.Commit();
    }

    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out LoadoutWithTxId id)
    {
        if (_lastAppliedRevisionDictionary.TryGetValue(gameInstallation, out var lastAppliedLoadout))
        {
            var conn = _connection.AsOf(lastAppliedLoadout.Tx);
            var model = conn.Get<Loadout.ReadOnly>(lastAppliedLoadout.Id.Value);
            id = lastAppliedLoadout;
            return model.LoadoutKind != LoadoutKind.Deleted;
        }

        var diskStateTree = GetState(gameInstallation);
        if (diskStateTree is null)
        {
            id = default(LoadoutWithTxId);
            return false;
        }
        
        id = new LoadoutWithTxId { Id = LoadoutId.From(diskStateTree.LoadoutId), Tx = diskStateTree.TxId };
        _lastAppliedRevisionDictionary[gameInstallation] = id;
        return true;
    }
    
    private static DiskState.ReadOnly? PreviousStateEntity(IDb db, GameInstallation gameInstallation)
    {
        return DiskState.FindByRoot(db, gameInstallation.LocationsRegister[LocationId.Game].ToString())
            .FirstOrDefault(state => state.Game == gameInstallation.Game.Domain);
    }
}
