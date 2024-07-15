using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;

namespace NexusMods.Library;

[UsedImplicitly]
internal class AddLocalFileJobWorker : AJobWorker<AddLocalFileJob>
{
    private readonly IServiceProvider _serviceProvider;

    public AddLocalFileJobWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<JobResult> ExecuteAsync(AddLocalFileJob job, CancellationToken cancellationToken)
    {
        var absolutePath = job.FilePath;

        var addLibraryFileJob = new AddLibraryFileJob(job, _serviceProvider.GetRequiredService<AddLibraryFileJobWorker>())
        {
            Transaction = job.Transaction,
            FilePath = job.FilePath,
            DoCommit = false,
        };

        var jobResult = await AddJobAndWaitForResultAsync(addLibraryFileJob);
        var libraryFile = RequireDataFromResult<LibraryFile.New>(jobResult);

        var localFile = new LocalFile.New(job.Transaction, libraryFile.LibraryFileId)
        {
            LibraryFile = libraryFile,
            OriginalPath = absolutePath.ToString(),
        };

        var transactionResult = await job.Transaction.Commit();
        return JobResult.CreateCompleted(transactionResult.Remap(localFile));
    }
}
