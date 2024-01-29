using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A service that can list tools for games and run them
/// </summary>
public interface IToolManager
{
    /// <summary>
    /// Get all tools that can be run for a given loadout
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public IEnumerable<ITool> GetTools(Loadout loadout);

    /// <summary>
    /// Run a tool for a given loadout. Returns the modified loadout. Generated files will be added to the given mod,
    /// or a new mod if none is provided.
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="loadout"></param>
    /// <param name="generatedFilesMod"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Loadout> RunTool(ITool tool, Loadout loadout, ModId? generatedFilesMod = null,
        CancellationToken token = default);
}
