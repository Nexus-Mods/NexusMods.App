using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddDownloadJob : IJobDefinitionWithStart<AddDownloadJob, LibraryFile.ReadOnly>, IAddDownloadJob
{ 
    public required IJobTask<IDownloadJob, AbsolutePath> DownloadJob { get; init; }
    internal required IConnection Connection { get; set; }
    internal required IServiceProvider ServiceProvider { get; set; }
    
    public static IJobTask<AddDownloadJob, LibraryFile.ReadOnly> Create(IServiceProvider provider, IJobTask<IDownloadJob, AbsolutePath> downloadJob)
    {
        var monitor = provider.GetRequiredService<IJobMonitor>();
        var job = new AddDownloadJob
        {
            DownloadJob = downloadJob,
            Connection = provider.GetRequiredService<IConnection>(),
            ServiceProvider = provider,
        };
        return monitor.Begin<AddDownloadJob, LibraryFile.ReadOnly>(job);
    }

    public async ValueTask<LibraryFile.ReadOnly> StartAsync(IJobContext<AddDownloadJob> context)
    {
        await context.YieldAsync();
        await DownloadJob;

        await context.YieldAsync();
        using var tx = Connection.BeginTransaction();

        var libraryFile = await AddLibraryFileJob.Create(ServiceProvider, tx, DownloadJob.Result, true, false);
        await DownloadJob.Job.AddMetadata(tx, libraryFile);

        var transactionResult = await tx.Commit();
        return transactionResult.Remap(libraryFile);
    }
}
