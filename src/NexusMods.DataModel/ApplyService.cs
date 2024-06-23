﻿using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.DataModel.Loadouts.Extensions;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel;

/// <inheritdoc />
public class ApplyService : IApplyService
{
    private readonly ILogger<ApplyService> _logger;
    private readonly IDiskStateRegistry _diskStateRegistry;
    private readonly IConnection _conn;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ApplyService(IDiskStateRegistry diskStateRegistry, IConnection conn, ILogger<ApplyService> logger)
    {
        _logger = logger;
        _conn = conn;
        _diskStateRegistry = diskStateRegistry;
    }

    /// <inheritdoc />
    public async Task Apply(Loadout.ReadOnly loadout)
    {
        // TODO: Check if this or any other loadout is being applied to this game installation
        // Queue the loadout to be applied if that is the case.

        _logger.LogInformation(
            "Applying loadout {Name} to {GameName} {GameVersion}",
            loadout.Name,
            loadout.InstallationInstance.Game.Name,
            loadout.InstallationInstance.Version
        );

        try
        {
            await loadout.Apply();
        }
        catch (NeedsIngestException)
        {
            _logger.LogInformation("Ingesting loadout {Name} from {GameName} {GameVersion}", loadout.Name,
                loadout.InstallationInstance.Game.Name, loadout.InstallationInstance.Version
            );

            if (TryGetLastAppliedLoadout(loadout.InstallationInstance, out var lastAppliedLoadout))
            {
                _logger.LogInformation("Last applied loadout found: {LoadoutId} as of {TxId}", lastAppliedLoadout.Id, lastAppliedLoadout.MostRecentTxId());
            }
            else
            {
                // There is apparently no last applied revision, so we'll just use the loadout we're trying to apply
                lastAppliedLoadout = loadout;
            }

            var loadoutWithIngest = await loadout.Ingest();

            await loadoutWithIngest.Apply();
        }
    }


    /// <inheritdoc />
    public ValueTask<FileDiffTree> GetApplyDiffTree(Loadout.ReadOnly loadout)
    {
        var prevDiskState = _diskStateRegistry.GetState(loadout.InstallationInstance)!;
            
        var syncrhonizer = loadout.InstallationInstance.GetGame().Synchronizer;
        
        return syncrhonizer.LoadoutToDiskDiff(loadout, prevDiskState);
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> Ingest(GameInstallation gameInstallation)
    {
        if (!TryGetLastAppliedLoadout(gameInstallation, out var lastAppliedRevision))
        {
            throw new InvalidOperationException("Game installation does not have a last applied loadout to ingest into");
        }
        
        var loadoutWithIngest = await gameInstallation.GetGame().Synchronizer.Ingest(lastAppliedRevision);

        return loadoutWithIngest;
    }

    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out Loadout.ReadOnly loadout)
    {
        if (!_diskStateRegistry.TryGetLastAppliedLoadout(gameInstallation, out var lastId))
        {
            loadout = default(Loadout.ReadOnly);
            return false;
        }
        
        var db = _conn.AsOf(lastId.Tx);
        loadout = Loadout.Load(db, lastId.Id);
        return true;
    }

    /// <inheritdoc />
    public IObservable<LoadoutWithTxId> LastAppliedRevisionFor(GameInstallation gameInstallation)
    {
        LoadoutWithTxId last;
        if (_diskStateRegistry.TryGetLastAppliedLoadout(gameInstallation, out var lastId))
            last = lastId;
        else
            last = new LoadoutWithTxId(LoadoutId.From(EntityId.From(0)), TxId.From(0));
        
        // Return a deferred observable that computes the starting value only on first subscription
        return Observable.Defer(() => _diskStateRegistry.LastAppliedRevisionObservable
            .Where(x => x.Install.Equals(gameInstallation))
            .Select(x => x.LoadoutRevisionId)
            .StartWith(last)
        );
    }
}
