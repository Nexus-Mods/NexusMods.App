using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddDownloadJobWorker : AJobWorker<AddDownloadJob>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJobMonitor _jobMonitor;
    private readonly IConnection _connection;

    public AddDownloadJobWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    protected override async Task<JobResult> ExecuteAsync(AddDownloadJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!job.DownloadJobResult.HasValue)
        {
            await job.StartAsync(cancellationToken: cancellationToken);
            var result = await job.DownloadJob.WaitToFinishAsync(cancellationToken: cancellationToken);
            job.DownloadJobResult = result.RequireData<AbsolutePath>();
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var tx = _connection.BeginTransaction();

        await using var addLibraryFileJob = new AddLibraryFileJob(group: job, monitor: _jobMonitor, worker: _serviceProvider.GetRequiredService<AddLibraryFileJobWorker>())
        {
            Transaction = tx,
            FilePath = job.DownloadJobResult.Value,
            DoBackup = true,
            DoCommit = false,
        };

        await addLibraryFileJob.StartAsync(cancellationToken: cancellationToken);
        var addLibraryFileJobResult = await addLibraryFileJob.WaitToFinishAsync(cancellationToken: cancellationToken);

        var libraryFile = addLibraryFileJobResult.RequireData<LibraryFile.New>();
        job.DownloadJob.AddMetadata(tx, libraryFile);

        var transactionResult = await tx.Commit();
        return JobResult.CreateCompleted(transactionResult.Remap(libraryFile));
    }
}
