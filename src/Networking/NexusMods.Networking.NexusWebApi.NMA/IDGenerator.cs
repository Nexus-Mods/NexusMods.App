namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// Concrete implementation for id generator using standard libraries as far as possible
/// </summary>
// ReSharper disable once InconsistentNaming
public class IDGenerator
{
    public static string UUIDv4()
    {
        return Guid.NewGuid().ToString("N");
    }
}
