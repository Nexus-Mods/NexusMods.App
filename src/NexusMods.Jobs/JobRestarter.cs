using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Jobs;

public class JobRestarter(IConnection connection) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var jobsToRestart = PersistedJobState
            .FindByStatus(connection.Db, JobStatus.Running)
            .Concat(PersistedJobState.FindByStatus(connection.Db, JobStatus.Paused))
            .Concat(PersistedJobState.FindByStatus(connection.Db, JobStatus.Created));

        foreach (var job in jobsToRestart)
        {
            var worker = PersistedJobState.Worker.GetWorker(job);
            worker.StartAsync(job, CancellationToken.None);
        }

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
