using System.Drawing;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Paths;
using Size = NexusMods.Paths.Size;

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
    /// <returns></returns>
    public static ChunkState Create(Size start, Size size)
    {
        return new ChunkState
        {
            Completed = Size.Zero,
            Read = Size.Zero,
            Size = size,
            Offset = start,
        };
    }

    /// <summary>
    /// The offset of the chunk within the output file
    /// </summary>
    public Size Offset { get; init; } = Size.Zero;

    /// <summary>
    /// The size of the chunk
    /// </summary>
    public Size Size { get; set; }

    /// <summary>
    /// The number of bytes read from the network into the chunk
    /// </summary>
    [JsonIgnore]
    public Size Read { get; set; } = Size.Zero;

    /// <summary>
    /// The number of bytes written to the output file
    /// </summary>
    public Size Completed { get; set; } = Size.Zero;

    /// <summary>
    /// The source of the chunk, (the download information)
    /// </summary>
    [JsonIgnore]
    public Source? Source { get; set; }

    /// <summary>
    /// The source URL of the chunk
    /// </summary>
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
    public Bandwidth BytesPerSecond => Read / (DateTime.Now - Started);

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
    public Size RemainingToRead => Size - Read;
}
