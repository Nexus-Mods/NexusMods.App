namespace NexusMods.CLI.Types;

/// <summary>
/// Defines a protocol handler used for downloading items.
/// </summary>
public interface IDownloadProtocolHandler
{
    /// <summary>
    /// The protocol to handle, e.g. 'nxm'
    /// </summary>
    public string Protocol { get; }
    
    /// <summary>
    /// Handles downloads from the given URL.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="token">Allows to cancel the operation.</param>
    public Task Handle(string url, CancellationToken token);
}
