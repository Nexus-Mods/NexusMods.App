namespace NexusMods.Abstractions.Jobs;

public abstract class AJobGroupWorker<TJobGroup> : AJobWorker<TJobGroup>
    where TJobGroup : AJobGroup
{
    protected TJobGroup JobGroup { get; }

    protected AJobGroupWorker(TJobGroup jobGroup) : base(jobGroup)
    {
        JobGroup = jobGroup;
    }

    protected Task<JobResult> AddJobAndWaitForResultAsync(AJob job)
    {
        throw new NotImplementedException();
    }

    protected TData RequireDataFromResult<TData>(JobResult result)
    {
        throw new NotImplementedException();
    }

    protected Task<JobResult[]> AddJobsAndWaitParallelAsync(AJob[] jobs)
    {
        throw new NotImplementedException();
    }
}
