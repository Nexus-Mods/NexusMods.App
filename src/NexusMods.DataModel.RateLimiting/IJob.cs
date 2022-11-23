namespace NexusMods.DataModel.RateLimiting;

public interface IJob
{
    public ulong ID { get; }
    public long? Size { get; set; }
    public long Current { get; }
    public string Description { get; }
    public ValueTask Report(int processedSize, CancellationToken token);
    public void ReportNoWait(int processedSize);
}

public interface IJob<T>
{
    public Type Type => typeof(T);
}