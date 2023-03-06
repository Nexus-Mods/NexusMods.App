namespace NexusMods.DataModel.RateLimiting;

public record StatusReport<TUnit>(int Running, int Pending, TUnit Transferred)
{
}
