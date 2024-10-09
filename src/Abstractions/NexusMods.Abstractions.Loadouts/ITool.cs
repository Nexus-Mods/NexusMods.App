using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Specifies a tool that is run outside of the app. Could be the game itself,
/// a file generator, some sort of editor, patcher, etc.
/// </summary>
public interface ITool
{
    /// <summary>
    /// List of supported game IDs.
    /// </summary>
    public IEnumerable<GameId> GameIds { get; }

    /// <summary>
    /// Human friendly name of the tool.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Executes this tool against the given loadout.
    /// </summary>
    public Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken);
}
