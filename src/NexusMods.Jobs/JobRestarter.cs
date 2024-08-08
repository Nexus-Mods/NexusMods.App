using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Jobs;

public class JobRestarter(IConnection connection, ILogger<JobRestarter> logger) : IHostedService
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
            try
            {
                // TODO: can't start running jobs
                var job = jobState.Worker.LoadJob(jobState);
                await job.StartAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to restart job {JobId}", jobState.Id);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
