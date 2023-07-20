using System.Text.Json.Serialization;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.DTOs;

/// <summary>
/// Overall state of a download
/// </summary>
class DownloadState
{
    
    /// <summary>
    /// Total size of the download
    /// </summary>
    public long TotalSize { get; set; } = -1;

    /// <summary>
    /// A list of the chunks that make up the download
    /// </summary>
    public List<ChunkState> Chunks { get; set; } = new();

    /// <summary>
    /// The destination of the download on the local file system
    /// </summary>
    [JsonIgnore]
    public AbsolutePath Destination;
    
    /// <summary>
    /// The sources of the download (the download information, aka URL mirrors)
    /// </summary>
    [JsonIgnore]
    public Source[]? Sources;
}