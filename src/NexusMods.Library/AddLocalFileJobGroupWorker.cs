using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;

namespace NexusMods.Library;

internal class AddLocalFileJobGroupWorker : AJobGroupWorker<AddLocalFileJobGroup>
{
    private readonly IServiceProvider _serviceProvider;

    public AddLocalFileJobGroupWorker(
        IServiceProvider serviceProvider,
        AddLocalFileJobGroup job) : base(job)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var absolutePath = JobGroup.FilePath;

        var job = new AddLibraryFileJobGroup(JobGroup, _serviceProvider.GetRequiredService<AddLibraryFileJobGroupWorker>())
        {
            Transaction = JobGroup.Transaction,
            FilePath = JobGroup.FilePath,
            DoCommit = false,
        };

        var jobResult = await AddJobAndWaitForResultAsync(job);
        var libraryFile = RequireDataFromResult<LibraryFile.New>(jobResult);

        var localFile = new LocalFile.New(JobGroup.Transaction, libraryFile.LibraryFileId)
        {
            LibraryFile = libraryFile,
            OriginalPath = absolutePath.ToString(),
        };

        var transactionResult = await JobGroup.Transaction.Commit();
        return CompleteJob(transactionResult.Remap(localFile));
    }
}
