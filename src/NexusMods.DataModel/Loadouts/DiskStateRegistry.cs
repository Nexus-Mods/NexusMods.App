using System.Diagnostics;
using System.Reactive.Subjects;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Serialization.DataModel;
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
        // I looked for a 'clean' way to do this.
        // I originally considered special casing this to `IdEmpty.Empty`, or a
        // equivalent 'null'/default ID.

        // Although putting it in the `LayoutRoots` table should be ok, as the
        // probability of a collision is very low (the chances of something
        // hashing to '0' are very low), I figured, that for cleanliness,
        // maintainability and logical consistency, it would be better to
        // put the initial states in a separate table, so here we are.

        // - Sewer

        // TODO: This implementation does not support multiple installs of the same game,
        // e.g. a GOG version alongside Steam version.
        // But our app currently does not handle that well either.
        var tx = _connection.BeginTransaction();
        var domain = installation.Game.Domain;
        _ = new DiskState.Model(tx)
        {
            Game = domain,
            Root = installation.LocationsRegister[LocationId.Game].ToString(),
            LoadoutRevision = GetDiskStateIdForDomain(domain),
            DiskState = diskState,
        };

        await tx.Commit();
    }

    /// <inheritdoc />
    public DiskStateTree? GetInitialState(GameInstallation installation)
    {
        // Initial state is identified by an empty LoadoutRevision
        var db = _connection.Db;
        var domain = installation.Game.Domain;
        var domainId = GetDiskStateIdForDomain(domain);
        var result = FindHistoryIndexed(db, domainId, DiskState.LoadoutRevision)
            .Select(db.Get<DiskState.Model>)
            .FirstOrDefault();

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

    private static Id64 GetDiskStateIdForDomain(GameDomain domain)
    {
        return new Id64(EntityCategory.InitialDiskStates, domain.GetStableHash());
    }
    
    // Gets all entities in the history index that match a given attribute.
    private IEnumerable<EntityId> FindHistoryIndexed<TValue, TLowLevel>(IDb db, TValue value, Attribute<TValue, TLowLevel> attribute)
    {
        // TODO: Discuss and move this to MneumonicDB or an extension
        //       method library.
        attribute.GetDbId(db.Registry.Id);
        if (!attribute.IsIndexed)
            throw new InvalidOperationException($"Attribute {attribute.Id} is not indexed");

        using var start = new PooledMemoryBufferWriter(64);
        attribute.Write(EntityId.MinValueNoPartition, db.Registry.Id, value, TxId.MinValue, false, start);

        using var end = new PooledMemoryBufferWriter(64);
        attribute.Write(EntityId.MaxValueNoPartition, db.Registry.Id, value, TxId.MinValue, false, end);

        var results = db.Snapshot
            .Datoms(IndexType.AVETHistory, start.GetWrittenSpan(), end.GetWrittenSpan())
            .Select(d => d.E);

        return results;
    }
}
