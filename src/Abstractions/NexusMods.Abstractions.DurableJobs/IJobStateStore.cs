namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// Storage for the state of jobs
/// </summary>
public interface IJobStateStore
{
    /// <summary>
    /// Writes the state of a job.
    /// </summary>
    public void Write(JobId jobId, byte[] state);
    
    /// <summary>
    /// Reads the state of a job.
    /// </summary>
    public byte[] Read(JobId jobId);
    
    /// <summary>
    /// Deletes the state of a job.
    /// </summary>
    public void Delete(JobId jobId);
    
    /// <summary>
    /// All the job ids.
    /// </summary>
    public IEnumerable<JobId> All();
}
