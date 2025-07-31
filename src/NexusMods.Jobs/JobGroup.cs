using System.Collections;
using System.Collections.Concurrent;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Jobs;

public class JobGroup : IJobGroup
{
    private readonly JobCancellationToken _jobCancellationToken;
    
    public JobGroup(bool supportsForcePause = false) => _jobCancellationToken = new JobCancellationToken(supportsForcePause);

    public bool IsCancelled => _jobCancellationToken.Token.IsCancellationRequested;

    private ConcurrentBag<IJob> Jobs { get; } = new();
    public IEnumerator<IJob> GetEnumerator() => Jobs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Attach(IJob job) => Jobs.Add(job);

    public int Count => Jobs.Count;
    public JobCancellationToken JobCancellationToken => _jobCancellationToken;
    public CancellationToken CancellationToken => _jobCancellationToken.Token;
    public void Cancel() => _jobCancellationToken.Cancel();
    public void Pause() => _jobCancellationToken.Pause();
    public void Resume() => _jobCancellationToken.Resume();
}
