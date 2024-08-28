using System.Collections.Immutable;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs.StateStore;

public record HistoryEntry
{
    /// <summary>
    /// The Id of the child job.
    /// </summary>
    public JobId ChildJobId { get; init; }
    
    /// <summary>
    /// The current state of the child job, if it's not completed yet it will be <see cref="JobState.Running"/> otherise it will be <see cref="JobState.Completed"/>
    /// or <see cref="JobState.Failed"/>.
    /// </summary>
    public JobState State { get; init; }
    
    /// <summary>
    /// If the job is completed, this will contain the result of the job, if it's failed it will contain the exception.
    /// </summary>
    public object Result { get; init; } = default!;
}

public record JobHistory
{
    /// <summary>
    /// The Id of the job.
    /// </summary>
    public JobId JobId { get; init; }
    
    /// <summary>
    /// The state of the job.
    /// </summary>
    public JobState State { get; init; }
    
    /// <summary>
    /// The Id of the parent job, if this job is a root job this will be null.
    /// </summary>
    public JobId? ParentJobId { get; init; }
    
    /// <summary>
    /// The arguments that were passed to the job.
    /// </summary>
    public object[] Arguments { get; init; } = [];
    
    /// <summary>
    /// The class type of the job.
    /// </summary>
    public Type JobType { get; init; } = default!;
    
    /// <summary>
    /// A continuation that will be called when the job is completed, if this job was hydrated from the state store this will be null as the job is essentially
    /// orphaned.
    /// </summary>
    public Action<object, Exception?>? Continuation { get; init; } = default!;
    
    /// <summary>
    /// The history of the job, each entry represents a child job that was run by this job.
    /// </summary>
    public ImmutableList<HistoryEntry> History { get; init; } = ImmutableList<HistoryEntry>.Empty;
    
    /// <summary>
    /// A mapping of job Ids to the time-indexed history of the job.
    /// </summary>
    public ImmutableDictionary<JobId, ushort> ChildJobs { get; init; } = ImmutableDictionary<JobId, ushort>.Empty;
}
