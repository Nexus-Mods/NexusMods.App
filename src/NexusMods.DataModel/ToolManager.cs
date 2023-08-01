using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel;

/// <summary>
/// Default implementation of <see cref="IToolManager"/>.
/// </summary>
public class ToolManager : IToolManager
{
    private readonly LoadoutSynchronizer _loadoutSynchronizer;
    private readonly ILookup<GameDomain,ITool> _tools;
    private readonly IDataStore _dataStore;
    private readonly LoadoutRegistry _loadoutRegistry;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="tools"></param>
    /// <param name="loadoutSynchronizer"></param>
    /// <param name="dataStore"></param>
    /// <param name="loadoutRegistry"></param>
    public ToolManager(IEnumerable<ITool> tools, LoadoutSynchronizer loadoutSynchronizer, IDataStore dataStore, LoadoutRegistry loadoutRegistry)
    {
        _dataStore = dataStore;
        _tools = tools.SelectMany(tool => tool.Domains.Select(domain => (domain, tool)))
            .ToLookup(t => t.domain, t => t.tool);
        _loadoutSynchronizer = loadoutSynchronizer;
        _loadoutRegistry = loadoutRegistry;
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

        var plan = await _loadoutSynchronizer.MakeApplySteps(loadout, token);
        await _loadoutSynchronizer.Apply(plan, token);

        await tool.Execute(loadout);
        var modName = $"{tool.Name} Generated Files";

        if (generatedFilesMod == null)
        {
            var mod = loadout.Mods.Values.FirstOrDefault(m => m.Name == modName) ??
                      new Mod
                      {
                          Name = modName,
                          Id = ModId.New(),
                          Files = EntityDictionary<ModFileId, AModFile>.Empty(_dataStore)
                      };

            if (!loadout.Mods.ContainsKey(mod.Id))
                loadout = _loadoutRegistry.Alter(loadout.LoadoutId, "Adding Generated Files Mod",
                    old => old with
                    {
                        Mods = old.Mods.With(mod.Id, mod)
                    });
            generatedFilesMod = mod.Id;
        }

        //var ingestPlan = await _loadoutSynchronizer.MakeIngestPlan(loadout, _ => generatedFilesMod.Value, token);
        //return await _loadoutSynchronizer.Ingest(ingestPlan, $"Updating {tool.Name} Generated Files");
        return loadout;
    }
}
