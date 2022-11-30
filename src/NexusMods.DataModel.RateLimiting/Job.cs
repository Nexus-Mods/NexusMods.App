using System.Numerics;

namespace NexusMods.DataModel.RateLimiting;

public class Job<TResource, TUnit> : IJob<TResource, TUnit>, IDisposable
where TUnit : IAdditionOperators<TUnit, TUnit, TUnit>
{
    public ulong Id { get; internal init; }
    public string Description { get; internal init; }
    public bool Started { get; internal set; }
    public IResource<TResource, TUnit> Resource { get; init; }

    private bool _isFinished;

    public void Dispose()
    {
        if (_isFinished) return;
        _isFinished = true;
        Resource.Finish(this);
    }

    public TUnit Current { get; internal set; }
    public TUnit? Size { get; set; }

    public async ValueTask Report(TUnit processedSize, CancellationToken token)
    {
        await Resource.Report(this, processedSize, token);
        Current += processedSize;
    }

    public void ReportNoWait(TUnit processedSize)
    {
        Resource.ReportNoWait(this, processedSize);
    }
}