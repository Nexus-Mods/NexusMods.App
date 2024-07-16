using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs.Tests;

public partial class SynchronousTests
{
    internal class MyJob : AJob
    {
        public MyJob(IJobGroup? group = default, IJobWorker? worker = default)
            : base(null!, group, worker) { }
    }
}

