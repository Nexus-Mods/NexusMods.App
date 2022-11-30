namespace NexusMods.DataModel.RateLimiting;

public interface IJob : IDisposable
{
    public ulong Id { get; }
    public string Description { get; }

}

public interface IJob<TSize> : IJob
{
    public TSize? Size { get; set; }
    public TSize Current { get; }
    public ValueTask Report(TSize processed, CancellationToken token);
    public void ReportNoWait(TSize processed);
}

public interface IJob<TResource, TSize> : IJob<TSize>
{
    public Type Type => typeof(TResource);
}