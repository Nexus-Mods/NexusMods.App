using NexusMods.Sdk.Hashes;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Steam.DTOs;

/// <summary>
/// A full, detailed manifest for a specific manifest id
/// </summary>
public class Manifest
{
    /// <summary>
    /// The gid of the manifest
    /// </summary>
    public required ManifestId ManifestId { get; init; }
    
    /// <summary>
    /// The files in the manifest
    /// </summary>
    public required FileData[] Files { get; init; }
    
    /// <summary>
    /// The depot id of the manifest
    /// </summary>
    public required DepotId DepotId { get; init; }
    
    /// <summary>
    /// The time the manifest was created
    /// </summary>
    public required DateTimeOffset CreationTime { get; init; }
    
    /// <summary>
    /// The size of all files in the manifest, compressed
    /// </summary>
    public required Size TotalCompressedSize { get; init; }
    
    /// <summary>
    /// The size of all files in the manifest, uncompressed
    /// </summary>
    public required Size TotalUncompressedSize { get; init; }
    
    public class FileData
    {
        /// <summary>
        /// The name of the file
        /// </summary>
        public RelativePath Path { get; init; }
        
        /// <summary>
        /// The size of the file, compressed
        /// </summary>
        public Size Size { get; init; }
        
        /// <summary>
        /// The Sha1 hash of the file
        /// </summary>
        public required Sha1Value Hash { get; init; }
        
        /// <summary>
        /// The chunks of the file
        /// </summary>
        public required Chunk[] Chunks { get; init; }
    }

    public class Chunk
    {
        /// <summary>
        /// The id of the chunk
        /// </summary>
        public required Sha1Value ChunkId { get; init; }

        /// <summary>
        /// The crc32 checksum of the chunk
        /// </summary>
        public required Crc32 Checksum { get; init; }
        
        /// <summary>
        /// The offset of the chunk in the resulting file
        /// </summary>
        public required ulong Offset { get; init; }
        
        /// <summary>
        /// The size of the chunk, compressed
        /// </summary>
        public required Size CompressedSize { get; init; }
        
        /// <summary>
        /// The size of the chunk, uncompressed
        /// </summary>
        public required Size UncompressedSize { get; init; }
        
    }
}
