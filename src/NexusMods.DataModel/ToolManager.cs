﻿using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Extensions;

namespace NexusMods.DataModel;

/// <summary>
/// Default implementation of <see cref="IToolManager"/>.
/// </summary>
public class ToolManager : IToolManager
{
    private readonly ILookup<GameDomain,ITool> _tools;
    private readonly IDataStore _dataStore;
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly ILogger<ToolManager> _logger;
    private readonly IApplyService _applyService;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="tools"></param>
    /// <param name="loadoutSynchronizer"></param>
    /// <param name="dataStore"></param>
    /// <param name="loadoutRegistry"></param>
    public ToolManager(ILogger<ToolManager> logger, IEnumerable<ITool> tools, IDataStore dataStore, LoadoutRegistry loadoutRegistry, IApplyService applyService)
    {
        _logger = logger;
        _dataStore = dataStore;
        _tools = tools.SelectMany(tool => tool.Domains.Select(domain => (domain, tool)))
            .ToLookup(t => t.domain, t => t.tool);
        _loadoutRegistry = loadoutRegistry;
        _applyService = applyService;
    }

    /// <inheritdoc />
    public IEnumerable<ITool> GetTools(Loadout loadout)
    {
        return _tools[loadout.Installation.Game.Domain];
    }

    /// <inheritdoc />
    public async Task<Loadout> RunTool(ITool tool, Loadout loadout, ModId? generatedFilesMod = null, CancellationToken token = default)
    {
        if (!tool.Domains.Contains(loadout.Installation.Game.Domain))
            throw new Exception("Tool does not support this game");

        _logger.LogInformation("Running tool {ToolName} for loadout {LoadoutId} on {GameName} {GameVersion}", tool.Name, loadout.LoadoutId, loadout.Installation.Game.Name, loadout.Installation.Version);
        await tool.Execute(loadout, token);

        // This will ingest the changes into the last applied loadout without but will not apply any loadout
        _logger.LogInformation("Ingesting loadout {LoadoutId} from {GameName} {GameVersion}", loadout.LoadoutId, loadout.Installation.Game.Name, loadout.Installation.Version);
        return await _applyService.Ingest(loadout.Installation);
    }
}
