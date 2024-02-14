using System.Text.Json.Serialization;
using NexusMods.Paths;
using Size = NexusMods.Paths.Size;

namespace NexusMods.Networking.HttpDownloader.DTOs;

/// <summary>
/// Overall state of a download
/// </summary>
class DownloadState
{

    /// <summary>
    /// This is incremented by 1 every time the state is loaded and the download is resumed
    /// </summary>
    public int ResumeCount { get; set; }

    /// <summary>
    /// Total size of the download
    /// </summary>
    public Size TotalSize { get; set; }

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

    /// <summary>
    /// Returns true if any chunk is incomplete
    /// </summary>
    [JsonIgnore]
    public bool HasIncompleteChunk
    {
        get {
            return Chunks.Any(chunk => !chunk.IsComplete);
        }
    }

    /// <summary>
    /// Returns the number of bytes that have been already been downloaded
    /// </summary>
    [JsonIgnore]
    public Size CompletedSize
    {
        get {
            return Chunks.Select(chunk => chunk.Completed).Aggregate((a, b) => a + b);
        }
    }

    /// <summary>
    /// The file where the download state is stored
    /// </summary>
    [JsonIgnore]
    public AbsolutePath StateFilePath => GetStateFilePath(Destination);

    /// <summary>
    /// The file where the download is stored while it is in progress
    /// </summary>
    [JsonIgnore]
    public AbsolutePath TempFilePath => GetTempFilePath(Destination);

    /// <summary>
    /// Based on the destination, get the path to the state file
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public static AbsolutePath GetStateFilePath(AbsolutePath destination) => destination.ReplaceExtension(new Extension(".progress"));

    /// <summary>
    /// Based on the destination, get the path to the temp file
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public static AbsolutePath GetTempFilePath(AbsolutePath destination) => destination.ReplaceExtension(new Extension(".downloading"));


    /// <summary>
    /// Gets chunks that are not completely downloaded and written to disk
    /// </summary>
    /// <returns></returns>
    [JsonIgnore]
    public IEnumerable<ChunkState> UnfinishedChunks => Chunks.Where(chunk => !chunk.IsComplete);
}
