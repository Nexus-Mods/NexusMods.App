namespace NexusMods.DataModel.RateLimiting;

public record StatusReport(int Running, int Pending, long Transferred)
{
}