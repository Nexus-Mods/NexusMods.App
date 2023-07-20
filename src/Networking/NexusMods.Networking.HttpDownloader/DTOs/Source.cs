namespace NexusMods.Networking.HttpDownloader.DTOs;

/// <summary>
/// Information about a download source (URL mirror)
/// </summary>
class Source
{
    /// <summary>
    /// Request for the download
    /// </summary>
    public HttpRequestMessage? Request { get; init; }
    
    /// <summary>
    /// The priority of the source, slower sources should have a lower priority
    /// </summary>
    public int Priority { get; set; }
    
    public override string ToString()
    {
        return Request?.RequestUri?.AbsoluteUri ?? "No URL";
    }
}