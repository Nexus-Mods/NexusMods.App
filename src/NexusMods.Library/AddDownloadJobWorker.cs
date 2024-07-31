using NexusMods.Abstractions.Jobs;

namespace NexusMods.Library;

internal class AddDownloadJobWorker : AJobWorker<AddDownloadJob>
{
    protected override Task<JobResult> ExecuteAsync(AddDownloadJob job, CancellationToken cancellationToken)
    {
        return Task.FromResult(JobResult.CreateCancelled());
    }
}
