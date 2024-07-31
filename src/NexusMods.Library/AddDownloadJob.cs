using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Library;

internal class AddDownloadJob : AJob
{
    public AddDownloadJob(IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default)
        : base(null!, group, worker, monitor) { }

    public required IDownloadJob DownloadJob { get; init; }
}
