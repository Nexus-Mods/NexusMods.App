using NexusMods.Sdk.Hashes;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Paths;
using SteamKit2;

namespace NexusMods.Networking.Steam;

public static class ManifestParser
{
    public static Manifest Parse(DepotManifest manifest)
    {
        var files = manifest.Files is null ? [] : manifest.Files.Select(file => new Manifest.FileData
        {
            Path = file.FileName,
            Size = Size.From(file.TotalSize),
            Hash = Sha1Value.From(file.FileHash),
            Chunks = file.Chunks.Select(chunk => new Manifest.Chunk
            {
                ChunkId = Sha1Value.From(chunk.ChunkID),
                Offset = chunk.Offset,
                CompressedSize = Size.From(chunk.CompressedLength),
                UncompressedSize = Size.From(chunk.UncompressedLength),
                Checksum = Crc32.From(chunk.Checksum),
            }).ToArray(),
        }).ToArray();

        return new Manifest
        {
            ManifestId = ManifestId.From(manifest.ManifestGID),
            TotalCompressedSize = Size.From(manifest.TotalCompressedSize),
            TotalUncompressedSize = Size.From(manifest.TotalUncompressedSize),
            CreationTime = manifest.CreationTime,
            DepotId = DepotId.From(manifest.DepotID),
            Files = files,
        };

    }
    
}
