using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

/// <summary>
/// Internal job state for a job. 
/// </summary>

public class JobState : AJobState
{
    /// <summary>
    /// The history of the job, each entry represents a child job that was run by this job.
    /// </summary>
    public List<HistoryEntry> History { get; set; } = new();
    
    /// <summary>
    /// If provided, this will be called when the job is completed.
    /// </summary>
    [JsonIgnore]
    public Action<object, Exception?>? Continuation { get; set; }
}
