using System.Runtime.CompilerServices;

namespace NexusMods.Abstractions.Jobs;

public class JobTask<TJobType, TResultType>
{
    
    public TJobType Job { get; }
    
    public TaskAwaiter<TResultType> GetAwaiter()
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Returns the result of the job, throws if the job is not completed.
    /// </summary>
    public TResultType Result { get; }
}
