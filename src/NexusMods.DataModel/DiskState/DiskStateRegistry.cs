using System.Diagnostics;
using System.Reactive.Subjects;
using DynamicData.Kernel;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.DiskState.Models;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using InitialDiskState = NexusMods.Abstractions.DiskState.Models.InitialDiskState;
using LoadoutDiskState = NexusMods.Abstractions.DiskState.Models.LoadoutDiskState;

namespace NexusMods.DataModel.DiskState;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public class DiskStateRegistry : IDiskStateRegistry
{
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
        Debug.Assert(diskState.LoadoutId.Partition != PartitionId.Temp, "diskState.LoadoutId must not be a temporary id");
        Debug.Assert(diskState.TxId != TxId.From(0), "diskState.LoadoutId must be set");
        
        var db = _connection.Db;
        var tx = _connection.BeginTransaction();

        var previous = PreviousStateEntity(db, installation); 
        
        // If we have a previous state, update it
        if (previous.HasValue)
        {
            LoadoutDiskStateHelpers.Update(previous.Value, tx, diskState);
        }
        else
        {
            var newId = tx.TempId();
            var newState = new Abstractions.DiskState.Models.DiskState.New(tx, newId)
            {
                Game = installation.Game.Domain,
                Root = installation.LocationsRegister[LocationId.Game].ToString(),
            };
            
            _ = new LoadoutDiskState.New(tx, newId)
            {
                DiskState = newState,
                LoadoutId = diskState.LoadoutId,
                TxId = diskState.TxId,
            };

            newState.AddAll(tx, diskState);

        }
        await tx.Commit();


        var id = new LoadoutWithTxId { Id = diskState.LoadoutId, Tx = diskState.TxId };
        _lastAppliedRevisionSubject.OnNext((installation, id));
    }

    /// <inheritdoc />
    public Optional<LoadoutDiskState.ReadOnly> GetState(GameInstallation gameInstallation)
    {
        var db = _connection.Db;
        return PreviousStateEntity(db, gameInstallation);
    }

    /// <inheritdoc />
    public async Task SaveInitialState(GameInstallation installation, DiskStateTree diskState)
    {
        var tx = _connection.BeginTransaction();
        var domain = installation.Game.Domain;
        var diskStateId = tx.TempId();

        var newDiskState = new Abstractions.DiskState.Models.DiskState.New(tx, diskStateId)
        {
            Game = domain,
            Root = installation.LocationsRegister[LocationId.Game].ToString(),
        };
        
        _ = new InitialDiskState.New(tx, diskStateId)
        {
            DiskState = newDiskState,
            GameId = installation.GameMetadataId,
        };

        newDiskState.AddAll(tx, diskState);
        
        await tx.Commit();
    }

    /// <inheritdoc />
    public Optional<InitialDiskState.ReadOnly> GetInitialState(GameInstallation installation)
    {
        var db = _connection.Db;

        var result = Abstractions.DiskState.Models.DiskState.FindByRoot(db, installation.LocationsRegister[LocationId.Game].ToString())
            .OfTypeInitialDiskState()
            .FirstOrDefault();

        if (result.IsValid())
            return result;

        return Optional<InitialDiskState.ReadOnly>.None;
    }

    /// <inheritdoc />
    public async Task ClearInitialState(GameInstallation installation)
    {
        var db = _connection.Db;
        var initialDiskState = GetInitialState(installation);
        
        if (!initialDiskState.HasValue)
            return;

        var tx = _connection.BeginTransaction();
        tx.Delete(initialDiskState.Value.Id, false);
        await tx.Commit();
    }

    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out LoadoutWithTxId id)
    {
        var diskStateTree = GetState(gameInstallation);
        if (!diskStateTree.HasValue)
        {
            id = default(LoadoutWithTxId);
            return false;
        }
        
        id = new LoadoutWithTxId { Id = LoadoutId.From(diskStateTree.Value.LoadoutId), Tx = diskStateTree.Value.TxId };
        return true;
    }
    
    private static Optional<LoadoutDiskState.ReadOnly> PreviousStateEntity(IDb db, GameInstallation gameInstallation)
    {
        var result =  Abstractions.DiskState.Models.DiskState.FindByRoot(db, gameInstallation.LocationsRegister[LocationId.Game].ToString())
            .Where(state => state.Game == gameInstallation.Game.Domain)
            .OfTypeLoadoutDiskState()
            .First();

        if (result.IsValid())
            return result;
        
        return Optional<LoadoutDiskState.ReadOnly>.None;
    }
}
