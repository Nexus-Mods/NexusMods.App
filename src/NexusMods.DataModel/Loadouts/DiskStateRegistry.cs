using System.Diagnostics;
using System.Reactive.Subjects;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.DataModel.Attributes;
using NexusMods.Extensions.Hashing;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;

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
        Debug.Assert(diskState.LoadoutId.Partition != PartitionId.Temp, "diskState.LoadoutId must not be a temporary id");
        Debug.Assert(diskState.TxId != TxId.From(0), "diskState.LoadoutId must be set");
        
        var db = _connection.Db;
        var tx = _connection.BeginTransaction();

        var previous = PreviousStateEntity(db, installation); 
        
        // If we have a previous state, update it
        if (previous.IsValid())
        {
            tx.Add(previous.Id, DiskState.Loadout, diskState.LoadoutId);
            tx.Add(previous.Id, DiskState.TxId, diskState.TxId);
            // Sync the contents
            DiskStateRoot.Update(previous.Db, tx, previous.State.Id,
                diskState.GetAllDescendentFiles().Select(f => f.Item.Value)
            ); 
        }
        else
        {
            
            var state = new DiskState.New(tx)
            {
                Game = installation.Game.Domain,
                Root = installation.LocationsRegister[LocationId.Game].ToString(),
                LoadoutId = diskState.LoadoutId,
                TxId = diskState.TxId,
                StateId = new DiskStateRoot.New(tx)
                {
                    GameInstallationId = installation.GameMetadataId,
                },
            };
            
            DiskStateRoot.Update(db, tx, state.StateId, diskState.GetAllDescendentFiles().Select(f => f.Item.Value));
        }
        await tx.Commit();


        var id = new LoadoutWithTxId { Id = diskState.LoadoutId, Tx = diskState.TxId };
        _lastAppliedRevisionDictionary[installation] = id;
        _lastAppliedRevisionSubject.OnNext((installation, id));
    }

    /// <inheritdoc />
    public async Task<DiskStateTree> GetState(GameInstallation gameInstallation)
    {
        var db = _connection.Db;
        var result = GameState.FindByGame(db, gameInstallation.GameMetadataId)
            .First();

        DiskStateTree tree;
        if (result.IsValid())
        { 
            using var tx = _connection.BeginTransaction();
            
            if (await ReindexState(gameInstallation, result.AsDiskStateRoot(), tx))
            {
                // Only commit if we have changes
                await tx.Commit();
                tree = result.Rebase().AsDiskStateRoot().ToTree(gameInstallation);
            }
            else
            {
                tree = result.AsDiskStateRoot().ToTree(gameInstallation);
            }
        }
        else
        {
            using var tx = _connection.BeginTransaction();
            var state = await IndexNewState(gameInstallation, tx);
            _ = new GameState.New(tx, state.Id)
            {
                GameId= gameInstallation.GameMetadataId,
                DiskStateRoot = state,
            };
            var commit = await tx.Commit();
            tree = commit.Remap(state).ToTree(gameInstallation);
        }
        return tree;
    }

    private async Task<DiskStateRoot.New> IndexNewState(GameInstallation installation, ITransaction tx)
    {
        var state = new DiskStateRoot.New(tx)
        {
            GameInstallationId = installation.GameMetadataId,
        };
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            foreach (var file in location.Value.EnumerateFiles())
            {
                var gamePath = installation.LocationsRegister.ToGamePath(file);
                var newHash = await file.XxHash64Async();
                _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                {
                    Path = gamePath,
                    Hash = newHash,
                    Size = file.FileInfo.Size,
                    LastModified = file.FileInfo.LastWriteTimeUtc,
                    RootId = state.Id,
                };
            }
        }

        return state;
    }

    private async Task<bool> ReindexState(GameInstallation installation, DiskStateRoot.ReadOnly state, ITransaction tx)
    {
        
        var seen = new HashSet<GamePath>();
        var inState = state.Entries.ToDictionary(e => e.Path);
        bool changes = false;
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            foreach (var file in location.Value.EnumerateFiles())
            {
                var gamePath = installation.LocationsRegister.ToGamePath(file);
                
                if (!inState.TryGetValue(gamePath, out var entry))
                {
                    var fileInfo = file.FileInfo;
                    
                    // If the files don't match, update the entry
                    if (fileInfo.LastWriteTimeUtc > entry.LastModified || fileInfo.Size != entry.Size)
                    {
                        var newHash = await file.XxHash64Async();
                        tx.Add(entry.Id, DiskStateEntry.Size, fileInfo.Size);
                        tx.Add(entry.Id, DiskStateEntry.Hash, newHash);
                        tx.Add(entry.Id, DiskStateEntry.LastModified, fileInfo.LastWriteTimeUtc);
                        changes = true;
                    }
                }
                else
                {
                    // No previous entry found, so create a new one
                    var newHash = await file.XxHash64Async();
                    _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                    {
                        Path = gamePath,
                        Hash = newHash,
                        Size = file.FileInfo.Size,
                        LastModified = file.FileInfo.LastWriteTimeUtc,
                        RootId = state.Id,
                    };
                    changes = true;
                }
                
            }
        }
        
        foreach (var entry in inState.Values)
        {
            if (seen.Contains(entry.Path))
                continue;
            tx.Retract(entry.Id, DiskStateEntry.Path, entry.Path);
            tx.Retract(entry.Id, DiskStateEntry.Hash, entry.Hash);
            tx.Retract(entry.Id, DiskStateEntry.Size, entry.Size);
            tx.Retract(entry.Id, DiskStateEntry.LastModified, entry.LastModified);
            tx.Retract(entry.Id, DiskStateEntry.Root, state.Id);
            changes = true;
        }
        
        return changes;
    }

    /// <inheritdoc />
    public async Task SaveInitialState(GameInstallation installation, DiskStateTree diskState)
    {
        var tx = _connection.BeginTransaction();
        var domain = installation.Game.Domain;
        var state = new InitialDiskState.New(tx)
        {
            Game = domain,
            Root = installation.LocationsRegister[LocationId.Game].ToString(),
            StateId = new DiskStateRoot.New(tx)
            {
                GameInstallationId = installation.GameMetadataId,
            }
        };
        
        DiskStateRoot.Update(_connection.Db, tx, state.StateId, diskState.GetAllDescendentFiles().Select(f => f.Item.Value));

        await tx.Commit();
    }

    /// <inheritdoc />
    public DiskStateTree? GetInitialState(GameInstallation installation)
    {
        var db = _connection.Db;

        return InitialDiskState.FindByRoot(db, installation.LocationsRegister[LocationId.Game].ToString())
            .Select(x => x.State.ToTree(installation))
            .FirstOrDefault(defaultValue: null);
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
            var db = _connection.AsOf(lastAppliedLoadout.Tx);
            var model = Loadout.Load(db, lastAppliedLoadout.Id);
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
    
    
    private static DiskState.ReadOnly PreviousStateEntity(IDb db, GameInstallation gameInstallation)
    {
        return DiskState.FindByRoot(db, gameInstallation.LocationsRegister[LocationId.Game].ToString())
            .FirstOrDefault(state => state.Game == gameInstallation.Game.Domain);
    }
}
