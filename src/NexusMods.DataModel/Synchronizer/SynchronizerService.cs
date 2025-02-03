﻿using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.Abstractions.Loadouts.Exceptions;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI;
using R3;

namespace NexusMods.DataModel.Synchronizer;

/// <inheritdoc />
public class SynchronizerService : ISynchronizerService
{
    private readonly ILogger<SynchronizerService> _logger;
    private readonly IConnection _conn;
    private readonly IGameRegistry _gameRegistry;
    private readonly Dictionary<EntityId, SynchronizerState> _gameStates;
    private readonly Dictionary<LoadoutId, SynchronizerState> _loadoutStates;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _lock = new();

    /// <summary>
    /// DI Constructor
    /// </summary>
    public SynchronizerService(IConnection conn, ILogger<SynchronizerService> logger, IGameRegistry gameRegistry)
    {
        _logger = logger;
        _conn = conn;
        _gameRegistry = gameRegistry;
        _gameStates = _gameRegistry.Installations.ToDictionary(e => e.Key, _ => new SynchronizerState());
        _loadoutStates = Loadout.All(conn.Db).ToDictionary(e => e.LoadoutId, _ => new SynchronizerState());
    }
    
    /// <inheritdoc />
    public FileDiffTree GetApplyDiffTree(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(_conn.Db, loadoutId);
        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        var metaData = GameInstallMetadata.Load(_conn.Db, loadout.InstallationInstance.GameMetadataId);
        var diskState = metaData.DiskStateAsOf(metaData.LastScannedDiskStateTransaction);
        return synchronizer.LoadoutToDiskDiff(loadout, diskState);
    }
    
    /// <inheritdoc />
    public async Task Synchronize(LoadoutId loadoutId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var loadout = Loadout.Load(_conn.Db, loadoutId);
            ThrowIfMainBinaryInUse(loadout);

            var loadoutState = GetOrAddLoadoutState(loadoutId);
            using var _ = loadoutState.WithLock();

            var gameState = GetOrAddGameState(loadout.InstallationInstance.GameMetadataId);
            using var _2 = gameState.WithLock();

            await loadout.InstallationInstance.GetGame().Synchronizer.Synchronize(loadout);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private SynchronizerState GetOrAddLoadoutState(LoadoutId loadoutId)
    {
        lock (_lock)
        {
            if (!_loadoutStates.TryGetValue(loadoutId, out var state))
            {
                _loadoutStates[loadoutId] = state = new SynchronizerState();
            }

            return state;
        }
    }
    
    private SynchronizerState GetOrAddGameState(EntityId gameId)
    {
        lock (_lock)
        {
            if (!_gameStates.TryGetValue(gameId, out var state))
            {
                _loadoutStates[gameId] = state = new SynchronizerState();
            }
            return state;
        }
    }
    
    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out Loadout.ReadOnly loadout)
    {
        var metadata = gameInstallation.GetMetadata(_conn);
        
        if (GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastId))
        {
            loadout = Loadout.Load(_conn.Db, lastId);
            return true;
        }
        
        loadout = default(Loadout.ReadOnly);
        return false;
    }

    /// <inheritdoc />
    public IObservable<LoadoutWithTxId> LastAppliedRevisionFor(GameInstallation gameInstallation)
    {
        return GameInstallMetadata.Observe(_conn, gameInstallation.GameMetadataId)
            .Select(metadata =>
                {
                    if (GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastId) 
                        && GameInstallMetadata.LastSyncedLoadoutTransaction.TryGetValue(metadata, out var txId))
                    {
                        return new LoadoutWithTxId(lastId, TxId.From(txId.Value));
                    }

                    return default(LoadoutWithTxId);
                }
            );
    }

    
    /// <inheritdoc />
    public IObservable<GameSynchronizerState> StatusForGame(GameInstallMetadataId gameInstallId)
    {
        var gameState = GetOrAddGameState(gameInstallId);
        return gameState.ObservableForProperty(s => s.Busy, skipInitial: false)
            .Select(e => e.Value ? GameSynchronizerState.Busy : GameSynchronizerState.Idle);
    } 
    
    private readonly Dictionary<LoadoutId, Observable<LoadoutSynchronizerState>> _statusObservables = new();
    private readonly SemaphoreSlim _statusSemaphore = new(1, 1);

    /// <inheritdoc />
    public async Task<IObservable<LoadoutSynchronizerState>> StatusForLoadout(LoadoutId loadoutId)
    {
        await _statusSemaphore.WaitAsync();
        try
        {
            // This observable may perform heavy diffing operation, so it needs to be shared between all subscribers
            if (_statusObservables.TryGetValue(loadoutId, out var observable)) return observable.AsSystemObservable();

            observable = CreateStatusObservable(loadoutId);
            _statusObservables[loadoutId] = observable;
            return observable.AsSystemObservable();
        }
        finally
        {
            _statusSemaphore.Release();
        }
    }

    private Observable<LoadoutSynchronizerState> CreateStatusObservable(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(_conn.Db, loadoutId);
        var loadoutState = GetOrAddLoadoutState(loadoutId);

        var isBusy = loadoutState.ObservePropertyChanged(l => l.Busy);

        var lastApplied = LastAppliedRevisionFor(loadout.InstallationInstance)
            .ToObservable()
            .Where(last => last != default(LoadoutWithTxId));

        var revisions = Loadout.RevisionsWithChildUpdates(_conn, loadoutId)
            .ToObservable()
            // Use DB transaction, since child updates are not part of the loadout
            .Select(rev => (loadout: rev, revDbTx: _conn.Db.BasisTxId));

        var statusObservable = isBusy.CombineLatest(lastApplied,
                revisions,
                (busy, last, rev) => (busy, last, rev.loadout, rev.revDbTx)
            )
            
            .DistinctUntilChanged()
            .SelectAwait(
                async (tuple, cancellationToken) =>
                {
                    var (busy, last, rev, revDbTx) = tuple;
                    
                    // if the loadout is not found, it means it was deleted
                    if (!rev.IsValid())
                        return LoadoutSynchronizerState.OtherLoadoutSynced;
                    
                    if (busy)
                        return LoadoutSynchronizerState.Pending;

                    if (last.Id != loadoutId)
                        return LoadoutSynchronizerState.OtherLoadoutSynced;

                    // Last DB revision is the same in the applied loadout
                    if (last.Id == rev.LoadoutId && revDbTx == last.Tx)
                        return LoadoutSynchronizerState.Current;

                    // Potentially long operation, run on thread pool
                    var diffFound = await Task.Run(() =>
                        {
                            _logger.LogTrace("Checking for changes in loadout {LoadoutId}", loadoutId);
                            var diffTree = GetApplyDiffTree(loadoutId);
                            var diffFound = diffTree.GetAllDescendentFiles().Any(f => f.Item.Value.ChangeType != FileChangeType.None);
                            _logger.LogTrace("Changes found in loadout {LoadoutId}: {DiffFound}", loadoutId, diffFound);
                            return diffFound;
                        }, cancellationToken);

                    return diffFound ? LoadoutSynchronizerState.NeedsSync : LoadoutSynchronizerState.Current;
                },
                awaitOperation: AwaitOperation.ThrottleFirstLast
            )
            .Replay(1)
            .RefCount();

        return statusObservable;
    }
    
    private void ThrowIfMainBinaryInUse(Loadout.ReadOnly loadout)
    {   
        // Note(sewer):
        // Problem: Game may already be running.
        // Edge Cases: - User may have multiple copies of a given game running.
        //             - Only on Windows.
        // Solution: Check if EXE (primaryfile) is in use.
        // Note: This doesn't account for CLI calls. I think that's fine; an external CLI user/caller
        var game = loadout.InstallationInstance.GetGame() as AGame;
        var primaryFile = game!.GetPrimaryFile(loadout.InstallationInstance.Store)
            .Combine(loadout.InstallationInstance.LocationsRegister[LocationId.Game]);
        if (IsFileInUse(primaryFile))
            throw new ExecutableInUseException("Game's main executable file is in use.\n" +
                                               "This is an indicator the game may have been started outside of the App; and therefore files may be in use.\n" +
                                               "This means that we are unable to perform a Synchronize (Apply) operation.");
        return;

        static bool IsFileInUse(AbsolutePath filePath)
        {
            if (!FileSystem.Shared.OS.IsWindows)
                return false;

            if (!filePath.FileExists)
                return false;

            try
            {
                using var fs = filePath.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                // The file is in use by another process
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // The file is in use or you don't have permission
                return true;
            }
        }
    }
}
