namespace NexusMods.Abstractions.DurableJobs;

/// <summary>
/// A unit of work is an isolated job task that cannot create sub-jobs and does not have its contents replayed (except for during
/// a total application restart).
/// </summary>
public interface IUnitOfWork
{
    
}
