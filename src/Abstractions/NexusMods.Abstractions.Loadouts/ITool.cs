using NexusMods.Abstractions.Games.DTO;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Specifies a tool that is run outside of the app. Could be the game itself,
/// a file generator, some sort of editor, patcher, etc.
/// </summary>
public interface ITool
{
    /// <summary>
    /// List of supported game IDs (domains).
    /// </summary>
    public IEnumerable<GameDomain> Domains { get; }

    /// <summary>
    /// Human friendly name of the tool.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Executes this tool against the given loadout.
    /// </summary>
    public Task Execute(Ids.LoadoutId loadout, CancellationToken cancellationToken);
}
