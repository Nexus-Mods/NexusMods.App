namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// abstractions for functionality that has no platform independent implementation in .NET
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IOSInterop
{
    /// <summary>
    /// open a url in the default application based on the protocol
    /// </summary>
    /// <param name="url">URI to open</param>
    /// <param name="fireAndForget">Start the process but don't wait for the completion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OpenUrl(Uri url, bool fireAndForget = false, CancellationToken cancellationToken = default);
}
