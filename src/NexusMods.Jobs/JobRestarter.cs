using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Jobs;

public class JobRestarter(IConnection connection) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var jobsToRestart = PersistedJobState
            .FindByStatus(connection.Db, JobStatus.Running)
            .Concat(PersistedJobState.FindByStatus(connection.Db, JobStatus.Paused))
            .Concat(PersistedJobState.FindByStatus(connection.Db, JobStatus.Created));

        foreach (var jobState in jobsToRestart)
        {
            var worker = PersistedJobState.Worker.GetWorker(jobState);
            var job = worker.LoadJob(jobState);
            await job.StartAsync(CancellationToken.None);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
