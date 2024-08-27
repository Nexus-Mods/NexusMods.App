using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public class CreateLoadoutJob : AJob
{
    public CreateLoadoutJob(IJobMonitor? monitor) : base(new MutableProgress(new IndeterminateProgress()), group: null, worker: null, monitor) { }

    public required GameInstallation Installation { get; init; }
}
