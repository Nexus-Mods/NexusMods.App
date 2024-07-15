using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;

namespace NexusMods.Library;

internal class AddLocalFileJobWorker : AJobWorker<AddLocalFileJob>
{
    private readonly IServiceProvider _serviceProvider;

    public AddLocalFileJobWorker(
        IServiceProvider serviceProvider,
        AddLocalFileJob job) : base(job)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var absolutePath = Job.FilePath;

        var job = new AddLibraryFileJob(Job, _serviceProvider.GetRequiredService<AddLibraryFileJobWorker>())
        {
            Transaction = Job.Transaction,
            FilePath = Job.FilePath,
            DoCommit = false,
        };

        var jobResult = await AddJobAndWaitForResultAsync(job);
        var libraryFile = RequireDataFromResult<LibraryFile.New>(jobResult);

        var localFile = new LocalFile.New(Job.Transaction, libraryFile.LibraryFileId)
        {
            LibraryFile = libraryFile,
            OriginalPath = absolutePath.ToString(),
        };

        var transactionResult = await Job.Transaction.Commit();
        return CompleteJob(transactionResult.Remap(localFile));
    }
}
