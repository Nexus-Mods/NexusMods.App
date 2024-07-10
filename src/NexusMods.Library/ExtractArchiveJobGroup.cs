using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class ExtractArchiveJobGroup : AJobGroup
{
    public ExtractArchiveJobGroup(IJobGroup? group = null, IJobWorker? worker = null) : base(group, worker) { }

    public required IStreamFactory FileStreamFactory { get; init; }
    public required AbsolutePath OutputPath { get; init; }
}
