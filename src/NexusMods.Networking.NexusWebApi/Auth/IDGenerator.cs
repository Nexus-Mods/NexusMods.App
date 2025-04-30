namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Concrete implementation for id generator using standard libraries as far as possible
/// </summary>
// ReSharper disable once InconsistentNaming
public class IDGenerator : IIDGenerator
{
    public string UUIDv4()
    {
        return Guid.NewGuid().ToString("N");
    }
}
