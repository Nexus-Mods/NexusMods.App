using System.Runtime.CompilerServices;

namespace NexusMods.Abstractions.Jobs;

public interface IJobTask<out TJobType, TResultType>
{
    public TJobType Job { get; }
    public TaskAwaiter<TResultType> GetAwaiter();
    
    /// <summary>
    /// Returns the result of the job, throws if the job is not completed.
    /// </summary>
    public TResultType Result { get; }
}
