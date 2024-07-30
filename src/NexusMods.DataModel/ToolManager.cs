using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.DataModel.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel;

/// <summary>
/// Default implementation of <see cref="IToolManager"/>.
/// </summary>
public class ToolManager : IToolManager
{
    private readonly ILookup<GameDomain,ITool> _tools;
    private readonly ILogger<ToolManager> _logger;
    private readonly ISynchronizerService _syncService;
    private readonly IConnection _conn;
    

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ToolManager(ILogger<ToolManager> logger, IEnumerable<ITool> tools, ISynchronizerService syncService, IConnection conn)
    {
        _logger = logger;
        _tools = tools.SelectMany(tool => tool.Domains.Select(domain => (domain, tool)))
            .ToLookup(t => t.domain, t => t.tool);
        _syncService = syncService;
        _conn = conn;
    }

    /// <inheritdoc />
    public IEnumerable<ITool> GetTools(Loadout.ReadOnly loadout)
    {
        return _tools[loadout.InstallationInstance.Game.Domain];
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> RunTool(
        ITool tool, 
        Loadout.ReadOnly loadout, 
        CancellationToken token = default)
    {
        if (!tool.Domains.Contains(loadout.InstallationInstance.Game.Domain))
            throw new Exception("Tool does not support this game");
        
        _logger.LogInformation("Applying loadout {LoadoutId} on {GameName} {GameVersion}", 
            loadout.Id, loadout.InstallationInstance.Game.Name, loadout.InstallationInstance.Version);
        await _syncService.Synchronize(loadout);
        var appliedLoadout = loadout.Rebase();

        _logger.LogInformation("Running tool {ToolName} for loadout {LoadoutId} on {GameName} {GameVersion}", 
            tool.Name, appliedLoadout.Id, appliedLoadout.InstallationInstance.Game.Name, appliedLoadout.InstallationInstance.Version);
        await tool.Execute(appliedLoadout, token);

        _logger.LogInformation("Ingesting loadout {LoadoutId} from {GameName} {GameVersion}", 
            appliedLoadout.Id, appliedLoadout.InstallationInstance.Game.Name, appliedLoadout.InstallationInstance.Version);
        await _syncService.Synchronize(appliedLoadout);

        return appliedLoadout.Rebase();
    }
}
