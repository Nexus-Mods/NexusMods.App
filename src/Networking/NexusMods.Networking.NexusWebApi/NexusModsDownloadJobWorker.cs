using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;

namespace NexusMods.Networking.NexusWebApi;

public class NexusModsDownloadJobWorker : APersistedJobWorker<NexusModsDownloadJob>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly IJobMonitor _jobMonitor;

    public NexusModsDownloadJobWorker(IServiceProvider serviceProvider, IConnection connection, IJobMonitor jobMonitor)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
        _jobMonitor = jobMonitor;
    }

    /// <inheritdoc/>
    public override Guid Id => Guid.Parse("06d5bac1-0f46-4e6e-8ccb-62b45f44eb2a");

    /// <inheritdoc/>
    protected override async Task<JobResult> ExecuteAsync(NexusModsDownloadJob job, CancellationToken cancellationToken)
    {
        // TODO: re-fetch download uri if expired
        await job.HttpDownloadJob.StartAsync(cancellationToken: cancellationToken);
        var res = await job.HttpDownloadJob.WaitToFinishAsync(cancellationToken: cancellationToken);
        return res;
    }

    /// <inheritdoc/>
    public override IJob LoadJob(PersistedJobState.ReadOnly state)
    {
        if (!state.TryGetAsHttpDownloadJobPersistedState(out var httpState))
            throw new NotSupportedException();

        if (!httpState.TryGetAsNexusModsDownloadJobPersistedState(out var nexusState))
            throw new NotSupportedException();

        var job = _serviceProvider.GetRequiredService<HttpDownloadJobWorker>().LoadJob(state);
        if (job is not HttpDownloadJob httpDownloadJob) throw new NotSupportedException();

        return new NexusModsDownloadJob(_connection, nexusState, worker: this, monitor: _jobMonitor)
        {
            HttpDownloadJob = httpDownloadJob,
        };
    }
}
