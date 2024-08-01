using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs.Tests;

public partial class SynchronousTests
{
    internal class MyJob : AJob
    {
        public MyJob(IJobGroup? group = default, IJobWorker? worker = default, IJobMonitor? monitor = default)
            : base(null!, group, worker, monitor) { }
    }
}

