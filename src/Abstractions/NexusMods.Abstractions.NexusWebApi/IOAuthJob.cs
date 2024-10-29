using NexusMods.Abstractions.Jobs;

namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// Represents a job for logging in using OAuth.
/// </summary>
public interface IOAuthJob : IJobDefinition, IDisposable
{
    R3.BehaviorSubject<Uri?> LoginUriSubject { get; }
}
