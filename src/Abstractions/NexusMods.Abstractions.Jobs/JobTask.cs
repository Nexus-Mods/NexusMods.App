using System.Runtime.CompilerServices;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A task-like object that represents a job and an eventual result.
/// </summary>
/// <typeparam name="TJobType">The type of the job</typeparam>
/// <typeparam name="TResultType">The eventual return type of the job</typeparam>
public interface IJobTask<out TJobType, TResultType>
    where TJobType : IJobDefinition<TResultType>
    where TResultType : notnull
{
    /// <summary>
    /// Returns the job that this task represents.
    /// </summary>
    public TJobType Job { get; }
    
    /// <summary>
    /// Returns an awaiter that will complete when the job is done.
    /// </summary>
    /// <returns></returns>
    public TaskAwaiter<TResultType> GetAwaiter();
    
    /// <summary>
    /// Returns the result of the job, throws if the job is not completed.
    /// </summary>
    public TResultType Result { get; }
}
