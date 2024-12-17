using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GOG.DTOs;

/// <summary>
/// Information about a depot, which is a collection of files and the chunks that make up those files.
/// </summary>
[UsedImplicitly]
public class DepotInfo
{
    
    /// <summary>
    /// The items in the depot.
    /// </summary>
    [JsonPropertyName("items")]
    public required DepotItem[] Items { get; init; }
    
    /// <summary>
    /// If the depot contains small files, this is the container that holds them.
    /// </summary>
    [JsonPropertyName("smallFilesContainer")]
    public SmallFilesContainer? SmallFilesContainer { get; init; }
}

/// <summary>
/// Information about a specific item in a depot.
/// </summary>
[UsedImplicitly]
public class DepotItem
{
    /// <summary>
    /// The path of the file, relative to the depot root.
    /// </summary>
    [JsonPropertyName("path")]
    public required RelativePath Path { get; init; }
    
    /// <summary>
    /// The chunks in the file.
    /// </summary>
    [JsonPropertyName("chunks")]
    public required Chunk[] Chunks { get; init; }
    
    /// <summary>
    /// The type of the depot item
    /// </summary>
    [JsonPropertyName("type")]
    public required DepotItemType Type { get; init; }

    /// <summary>
    /// A reference to the location of the file in the small file container, if the file is stored in a small files container.
    /// </summary>
    [JsonPropertyName("sfcRef")]
    public SmallFileContainerRef? SfcRef { get; init; }
}

/// <summary>
/// When there are many small files in a depot, they are grouped into a container, which acts like a single file
/// and the small files are stored as offsets within the container.
/// </summary>
[UsedImplicitly]
public class SmallFilesContainer
{
    /// <summary>
    /// The chunks in the container.
    /// </summary>
    [JsonPropertyName("chunks")]
    public required Chunk[] Chunks { get; init; }
}

/// <summary>
/// A chunk of a file
/// </summary>
public readonly struct Chunk
{
    /// <summary>
    /// The MD5 hash of the chunk.
    /// </summary>
    [JsonPropertyName("md5")]
    public required Md5 Md5 { get; init; }
    
    /// <summary>
    /// The uncompressed size of the chunk.
    /// </summary>
    [JsonPropertyName("size")]
    public required Size Size { get; init; }
    
    /// <summary>
    /// The Md5 hash of the compressed chunk.
    /// </summary>
    [JsonPropertyName("compressedMd5")]
    public required Md5 CompressedMd5 { get; init; }
    
    /// <summary>
    /// The compressed size of the chunk.
    /// </summary>
    [JsonPropertyName("compressedSize")]
    public required Size CompressedSize { get; init; }
}

/// <summary>
/// A reference to the location of a file in the small files container.
/// </summary>
[UsedImplicitly]
public class SmallFileContainerRef
{
    /// <summary>
    /// The offset of the file in the container.
    /// </summary>
    [JsonPropertyName("offset")]
    public Size Offset { get; init; }
    
    /// <summary>
    /// The size of the file in the container.
    /// </summary>
    [JsonPropertyName("size")]
    public Size Size { get; init; }
}
