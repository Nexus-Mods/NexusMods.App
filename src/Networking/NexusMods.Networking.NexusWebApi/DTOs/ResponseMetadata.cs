namespace NexusMods.Networking.NexusWebApi.DTOs;

/// <summary>
/// Metadata and rate limit information. Returned in the headers of every response.
/// </summary>
public class ResponseMetadata
{
    public int DailyLimit { get; set; }
    public int DailyRemaining { get; set; }
    public DateTime DailyReset { get; set; }
    public int HourlyLimit { get; set; }
    public int HourlyRemaining { get; set; }
    public DateTime HourlyReset { get; set; }
    public double Runtime { get; set; }

    public bool IsReal { get; set; } = true;
}