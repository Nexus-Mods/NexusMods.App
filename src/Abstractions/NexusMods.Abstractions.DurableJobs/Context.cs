namespace NexusMods.Abstractions.DurableJobs;

public class Context
{
    public required IJobManager JobManager { get; init; }
    public required JobId JobId { get; set; }
    public int HistoryIndex { get; set; } = 0;
}
