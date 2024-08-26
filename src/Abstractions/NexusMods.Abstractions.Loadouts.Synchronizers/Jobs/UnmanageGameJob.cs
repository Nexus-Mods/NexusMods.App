using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public class UnmanageGameJob : AJob
{
    public UnmanageGameJob(IJobMonitor? monitor) : base(new MutableProgress(new IndeterminateProgress()), group: null, worker: null, monitor) { }

    public required GameInstallation Installation { get; init; }
}
