using System.Numerics;

namespace NexusMods.DataModel.RateLimiting;

public class Job<TResource, TUnit> : IJob<TResource, TUnit>, IDisposable
where TUnit : IAdditionOperators<TUnit, TUnit, TUnit>, IDivisionOperators<TUnit, TUnit, double>
{
    public required ulong Id { get; init; }
    public required string Description { get; init; }
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
    public required IResource<TResource, TUnit> TypedResource { get; init; }
    public IResource Resource => TypedResource;

    private bool _isFinished;

    public void Dispose()
    {
        if (_isFinished) return;
        _isFinished = true;
        TypedResource.Finish(this);
    }

    public required TUnit Current { get; set; }
    public TUnit? Size { get; set; }

    public async ValueTask Report(TUnit processedSize, CancellationToken token)
    {
        await TypedResource.Report(this, processedSize, token);
        Current += processedSize;
    }

    public void ReportNoWait(TUnit processedSize)
    {
        TypedResource.ReportNoWait(this, processedSize);
    }
}