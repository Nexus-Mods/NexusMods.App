using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

/// <summary>
/// Internal job state for a job. 
/// </summary>

public class OrchestrationState : AJobState
{
    /// <summary>
    /// The history of the job, each entry represents a child job that was run by this job.
    /// </summary>
    public List<HistoryEntry> History { get; set; } = new();
}
