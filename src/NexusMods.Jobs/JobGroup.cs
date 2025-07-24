using System.Collections;
using System.Collections.Concurrent;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public class JobGroup : IJobGroup
{
    private readonly CancellationTokenSource _token = new();
    public bool IsCancelled => _token.Token.IsCancellationRequested;

    private ConcurrentBag<IJob> Jobs { get; } = new();
    public IEnumerator<IJob> GetEnumerator() => Jobs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Attach(IJob job) => Jobs.Add(job);

    public int Count => Jobs.Count;
    public CancellationToken CancellationToken => _token.Token;
    public void Cancel() => _token.Cancel();
}
