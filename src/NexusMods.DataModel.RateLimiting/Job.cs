using System.Numerics;

namespace NexusMods.DataModel.RateLimiting;

public class Job<TResource, TUnit> : IJob<TResource, TUnit>, IDisposable
where TUnit : IAdditionOperators<TUnit, TUnit, TUnit>, IDivisionOperators<TUnit, TUnit, double>
{
    public ulong Id { get; internal init; }
    public string Description { get; internal init; }
    public Percent Progress
    {
        get
        {
            if (!Started)
                return Percent.Zero;
            if (Size == null)
                return Percent.Zero;
            return new Percent(Current / Size);
        }
    }
    public bool Started { get; internal set; }
    public IResource<TResource, TUnit> _resource { get; init; }
    public IResource Resource => _resource;

    private bool _isFinished;

    public void Dispose()
    {
        if (_isFinished) return;
        _isFinished = true;
        _resource.Finish(this);
    }

    public TUnit Current { get; internal set; }
    public TUnit? Size { get; set; }

    public async ValueTask Report(TUnit processedSize, CancellationToken token)
    {
        await _resource.Report(this, processedSize, token);
        Current += processedSize;
    }

    public void ReportNoWait(TUnit processedSize)
    {
        _resource.ReportNoWait(this, processedSize);
    }
}