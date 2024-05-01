using System.Diagnostics;
using System.Reactive.Subjects;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Attributes;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public class DiskStateRegistry : IDiskStateRegistry
{
    private readonly IDictionary<GameInstallation, (EntityId, TxId)> _lastAppliedRevisionDictionary = new Dictionary<GameInstallation, (EntityId, TxId)>();
    private readonly Subject<(GameInstallation gameInstallation, EntityId, TxId)> _lastAppliedRevisionSubject = new();
    private readonly IConnection _connection;

    /// <inheritdoc />
    public IObservable<(GameInstallation gameInstallation, EntityId, TxId)> LastAppliedRevisionObservable => _lastAppliedRevisionSubject;

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

        _lastAppliedRevisionDictionary[installation] = (diskState.LoadoutId, diskState.TxId);
        _lastAppliedRevisionSubject.OnNext((installation, diskState.LoadoutId, diskState.TxId));
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
        var domain = installation.Game.Domain;
        
        // Find item with matching domain and root.
        // In practice it's unlikely you'd ever install more than one game at one
        // location, but since doing this check is virtually free, we might as well.
        return db.FindIndexed(installation.LocationsRegister[LocationId.Game].ToString(), InitialDiskState.Root)
            .Select(db.Get<InitialDiskState.Model>)
            .Where(x => x.Game == domain)
            .Select(x => x.DiskState)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out EntityId loadoutId, out TxId txId)
    {
        if (_lastAppliedRevisionDictionary.TryGetValue(gameInstallation, out var lastAppliedLoadout))
        {
            loadoutId = lastAppliedLoadout.Item1;
            txId = lastAppliedLoadout.Item2;
            return true;
        }

        var diskStateTree = GetState(gameInstallation);
        if (diskStateTree is null)
        {
            loadoutId = EntityId.MinValue;
            txId = TxId.MinValue;
            return false;
        }
        
        _lastAppliedRevisionDictionary[gameInstallation] = (diskStateTree.LoadoutId, diskStateTree.TxId);
        loadoutId = diskStateTree.LoadoutId;
        txId = diskStateTree.TxId;
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
