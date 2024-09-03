using System.Collections.Concurrent;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

public class InMemoryJobStore : IJobStateStore
{
    private ConcurrentDictionary<JobId, byte[]> _store = new();
    
    public void Write(JobId jobId, byte[] state)
    {
        _store[jobId] = state;
    }

    public byte[] Read(JobId jobId)
    {
        return _store[jobId];
    }

    public void Delete(JobId jobId)
    {
        _store.TryRemove(jobId, out _);
    }

    public IEnumerable<JobId> All()
    {
        return _store.Keys;
    }
}
