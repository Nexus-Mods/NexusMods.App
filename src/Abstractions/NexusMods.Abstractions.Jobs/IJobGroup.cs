namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A group of jobs
/// </summary>
public interface IJobGroup : IReadOnlyCollection<IJob>
{
    public CancellationToken CancellationToken { get; }
}
