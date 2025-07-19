using System.Collections;
using System.Collections.Concurrent;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public class JobGroup : IJobGroup
{
    private readonly CancellationTokenSource _token;
    private readonly JobMonitor _monitor;
    public bool IsCancelled => _token.Token.IsCancellationRequested;
    
    public JobGroup(JobMonitor monitor)
    {
        _token = new CancellationTokenSource();
        _monitor = monitor;
    }
    
    private ConcurrentBag<IJob> Jobs { get; } = new();
    public IEnumerator<IJob> GetEnumerator()
    {
        return Jobs.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public void Attach(IJob job)
    {
        Jobs.Add(job);
    }

    public int Count => Jobs.Count;
    public CancellationToken CancellationToken => _token.Token;
    public void Cancel() => _token.Cancel();
}
