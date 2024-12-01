using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public sealed class JobGroupCreator
{
    private static AsyncLocal<IJobGroup?> CurrentGroup { get; } = new();
    
    [MustDisposeResource] public static JobGroupDisposable Push(JobMonitor monitor)
    {
        return new JobGroupDisposable(monitor);
    }
    
    public struct JobGroupDisposable : IDisposable
    {
        private readonly IJobGroup? _previous;

        public JobGroupDisposable(JobMonitor monitor)
        {
            _previous = CurrentGroup.Value;
            Group = _previous ?? new JobGroup(monitor);
        }
        
        public IJobGroup Group { get; }
        
        public void Dispose()
        {
            CurrentGroup.Value = _previous;
        }
    }
    
}
