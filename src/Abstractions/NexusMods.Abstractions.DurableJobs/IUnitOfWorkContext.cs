namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// The context of a unit of work.
/// </summary>
public interface IUnitOfWorkContext
{
    /// <summary>
    /// The job id of this unit of work.
    /// </summary>
    public JobId JobId { get; set; }
    
    /// <summary>
    /// The job manager.
    /// </summary>
    public IJobManager JobManager { get; set; }

    /// <summary>
    /// Set the progress of the job.
    /// </summary>
    public void SetProgress(Percent? percent = null, double? ratePerSecond = null);
}
