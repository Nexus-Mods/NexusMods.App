using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.LibraryModels;

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

        var worker = _serviceProvider.GetRequiredService<AddLibraryFileJobWorker>();
        var addLibraryFileJob = new AddLibraryFileJob(job, worker)
        {
            Transaction = job.Transaction,
            FilePath = job.FilePath,
            DoCommit = false,
        };

        await worker.StartAsync(addLibraryFileJob, cancellationToken: cancellationToken);
        var jobResult = await addLibraryFileJob.WaitToFinishAsync(cancellationToken: cancellationToken);

        var libraryFile = jobResult.RequireData<LibraryFile.New>();
        var localFile = new LocalFile.New(job.Transaction, libraryFile.LibraryFileId)
        {
            LibraryFile = libraryFile,
            OriginalPath = absolutePath.ToString(),
        };

        var transactionResult = await job.Transaction.Commit();
        return JobResult.CreateCompleted(transactionResult.Remap(localFile));
    }
}
