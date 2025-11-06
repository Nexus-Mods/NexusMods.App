using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Jobs;

namespace NexusMods.DataModel;

/// <summary>
/// Default implementation of <see cref="IToolManager"/>.
/// </summary>
public class ToolManager : IToolManager
{
    private readonly ILookup<GameId, ITool> _tools;
    private readonly ILogger<ToolManager> _logger;
    private readonly ISynchronizerService _syncService;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ToolManager(ILogger<ToolManager> logger, IEnumerable<ITool> tools, ISynchronizerService syncService)
    {
        _logger = logger;
        _tools = tools.SelectMany(tool => tool.GameIds.Select(gameId => (gameId, tool)))
            .ToLookup(t => t.gameId, t => t.tool);
        _syncService = syncService;
    }

    /// <inheritdoc />
    public IEnumerable<ITool> GetTools(Loadout.ReadOnly loadout)
    {
        return _tools[loadout.LocatableGame.GameId];
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> RunTool(
        ITool tool, 
        Loadout.ReadOnly loadout, 
        IJobMonitor monitor,
        CancellationToken token = default)
    {
        if (!tool.GameIds.Contains(loadout.LocatableGame.GameId)) throw new NotSupportedException("Tool does not support this game");

        _logger.LogInformation("Applying loadout {LoadoutId} on {GameName} {GameVersion}", 
            loadout.Id, loadout.InstallationInstance.Game.DisplayName, loadout.GameVersion);
        await _syncService.Synchronize(loadout);
        var appliedLoadout = loadout.Rebase();

        _logger.LogInformation("Running tool {ToolName} for loadout {LoadoutId} on {GameName} {GameVersion}", 
            tool.Name, appliedLoadout.Id, appliedLoadout.InstallationInstance.Game.DisplayName, appliedLoadout.GameVersion);
        await tool.StartJob(appliedLoadout, monitor, token);

        _logger.LogInformation("Ingesting loadout {LoadoutId} from {GameName} {GameVersion}", 
            appliedLoadout.Id, appliedLoadout.InstallationInstance.Game.DisplayName, appliedLoadout.GameVersion);
        await _syncService.Synchronize(appliedLoadout);

        return appliedLoadout.Rebase();
    }
}
