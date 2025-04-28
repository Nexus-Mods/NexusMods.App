namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// abstraction for functions generating unique ids
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IIDGenerator
{
    /// <summary>
    /// generate a UUIDv4 <see href="https://datatracker.ietf.org/doc/html/rfc4122#section-4.4"/>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    string UUIDv4();
}
