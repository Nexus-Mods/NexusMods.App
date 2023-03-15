namespace NexusMods.Common.OSInterop;

/// <summary>
/// abstractions for functionality that has no platform independent implementation in .NET
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IOSInterop
{
    /// <summary>
    /// open a url in the default application based on the protocol
    /// </summary>
    /// <param name="url">url to open</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OpenUrl(string url, CancellationToken cancellationToken = default);
}
