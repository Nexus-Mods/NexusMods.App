using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Jobs;

public class JobRestarter(IConnection connection) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var db = connection.Db;
        var jobsToRestart = PersistedJobState
            .FindByStatus(db, JobStatus.Running)
            .Concat(PersistedJobState.FindByStatus(db, JobStatus.Paused))
            .Concat(PersistedJobState.FindByStatus(db, JobStatus.Created));

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
