using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace NexusMods.Networking.HttpDownloader.DTOs;


/// <summary>
/// Individual state of a chunk of a download
/// </summary>
public class ChunkState
{
    /// <summary>
    /// Create a new chunk state with the given size and offset
    /// </summary>
    /// <param name="start"></param>
    /// <param name="size"></param>
    /// <param name="initChunk"></param>
    /// <returns></returns>
    public static ChunkState Create(long start, long size)
    {
        return new ChunkState
        {
            Completed = 0,
            Read = 0,
            Size = size,
            Offset = start,
        };
    }

    /// <summary>
    /// The offset of the chunk within the output file
    /// </summary>
    public long Offset { get; init; }

    /// <summary>
    /// The size of the chunk
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The number of bytes read from the network into the chunk
    /// </summary>
    [JsonIgnore]
    public long Read { get; set; }

    /// <summary>
    /// The number of bytes written to the output file
    /// </summary>
    public long Completed { get; set; }

    /// <summary>
    /// The source of the chunk, (the download information)
    /// </summary>
    [JsonIgnore]
    public Source? Source { get; set; }

    [PublicAPI]
    public string SourceUrl => Source?.Request?.RequestUri?.AbsoluteUri ?? "No URL";

    /// <summary>
    /// Token used to cancel the download of the chunk
    /// </summary>
    [JsonIgnore]
    public CancellationTokenSource? Cancel { get; set; }

    /// <summary>
    /// The time the download of the chunk started
    /// </summary>
    [JsonIgnore]
    public DateTime Started { get; set; }

    /// <summary>
    /// The number of bytes per second being read from the network
    /// </summary>
    public int BytesPerSecond => (int)Math.Floor(Read / (DateTime.Now - Started).TotalSeconds);

    [JsonIgnore]
    public int KBytesPerSecond => (int)Math.Floor((Read / (DateTime.Now - Started).TotalSeconds) / 1024);


    /// <summary>
    /// True if the chunk has been read completely
    /// </summary>
    [JsonIgnore]
    public bool IsReadComplete => Read == Size;

    /// <summary>
    /// True if the chunk has been written completely
    /// </summary>
    [JsonIgnore]
    public bool IsWriteComplete => Completed == Size;

    /// <summary>
    /// True if the chunk has been read and written completely
    /// </summary>
    [JsonIgnore]
    public bool IsComplete => IsReadComplete && IsWriteComplete;

    /// <summary>
    /// Returns the number of bytes remaining to be read
    /// </summary>
    [JsonIgnore]
    public long RemainingToRead => Size - Read;
}
