using System.Numerics;

namespace NexusMods.DataModel.RateLimiting;

/// <summary>
/// A job is like a <see cref="Task{TResult}"/>, however provides additional information
/// (metadata) that allows for reporting progress.
/// </summary>
public class Job<TResource, TUnit> : IJob<TResource, TUnit>
where TUnit : IAdditionOperators<TUnit, TUnit, TUnit>, IDivisionOperators<TUnit, TUnit, double>
{
    private bool _isDisposed;

    /// <inheritdoc />
    public required ulong Id { get; init; }

    /// <inheritdoc />
    public required string Description { get; init; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool Started { get; internal set; }

    /// <summary>
    /// The resource which owns this job
    /// </summary>
    public required IResource<TResource, TUnit> TypedResource { get; init; }

    /// <inheritdoc />
    public IResource Resource => TypedResource;

    /// <inheritdoc />
    public required TUnit Current { get; set; }
    
    /// <inheritdoc />
    public TUnit? Size { get; set; }
    
    /// <inheritdoc />
    public DateTime StartedAt { get; set; }

    /// <inheritdoc />
    public required TUnit CurrentAtResumeTime { get; set; }

    /// <inheritdoc />
    public DateTime ResumedAt { get; set; }

    // TODO: Add finalizer here. https://github.com/Nexus-Mods/NexusMods.App/issues/211

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        TypedResource.Finish(this);
    }

    /// <inheritdoc />
    public async ValueTask ReportAsync(TUnit processedSize, CancellationToken token)
    {
        await TypedResource.ReportAsync(this, processedSize, token);
        Current += processedSize;
    }

    /// <inheritdoc />
    public void ReportNoWait(TUnit processedSize)
    {
        TypedResource.ReportNoWait(this, processedSize);
    }
}
