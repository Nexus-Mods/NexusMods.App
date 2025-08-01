using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public sealed class JobGroupCreator
{
    private static AsyncLocal<IJobGroup?> CurrentGroup { get; } = new();
    
    [MustDisposeResource] public static JobGroupDisposable Push()
    {
        return new JobGroupDisposable();
    }
    
    public struct JobGroupDisposable : IDisposable
    {
        private readonly IJobGroup? _previous;

        public JobGroupDisposable()
        {
            _previous = CurrentGroup.Value;
            Group = _previous ?? new JobGroup();
        }
        
        public IJobGroup Group { get; }
        
        public void Dispose()
        {
            CurrentGroup.Value = _previous;
        }
    }
    
}
