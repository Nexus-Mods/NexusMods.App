using NexusMods.Sdk.Games;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Loadouts;
using R3;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Specifies a tool that is run outside of the app. Could be the game itself,
/// a file generator, some sort of editor, patcher, etc.
/// </summary>
public interface ITool : IJobDefinition<Unit>
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
    /// Executes this tool against the given loadout using the <see cref="IJobMonitor"/>.
    /// </summary>
    /// <param name="loadout">The loadout to run the game with.</param>
    /// <param name="monitor">The monitor to which the task should be queued.</param>
    /// <param name="cancellationToken">Allows you to prematurely cancel the task.</param>
    public IJobTask<ITool, Unit> StartJob(Loadout.ReadOnly loadout, IJobMonitor monitor, CancellationToken cancellationToken);
}
