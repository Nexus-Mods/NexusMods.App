using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class ExtractArchiveJob : AJob
{
    public ExtractArchiveJob(IJobGroup? group = null, IJobWorker? worker = null, IJobMonitor? monitor = null)
        : base(null!, group, worker, monitor) { }

    public required IStreamFactory FileStreamFactory { get; init; }
    public required AbsolutePath OutputPath { get; init; }
}
