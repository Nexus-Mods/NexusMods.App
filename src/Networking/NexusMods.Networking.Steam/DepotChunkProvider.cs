using System.Buffers;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.IO.ChunkedStreams;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Paths;
using SteamKit2;

namespace NexusMods.Networking.Steam;

public class DepotChunkProvider : IChunkedStreamSource
{
    private readonly Manifest.FileData _fileData;
    private readonly Session _session;
    private readonly Manifest.Chunk[] _chunksSorted;
    private readonly AppId _appId;
    private readonly DepotId _depotId;

    public DepotChunkProvider(Session session, AppId appId, DepotId depotId, Manifest manifest, RelativePath relativePath)
    {
        _appId = appId;
        _depotId = depotId;
        _fileData = manifest.Files.First(f => f.Path == relativePath);
        _chunksSorted = _fileData.Chunks.OrderBy(c => c.Offset).ToArray();
        _session = session;
    }

    public Size Size => _fileData.Size;
    
    /// <summary>
    /// The Steam CDN provides data in 1MB chunks
    /// </summary>
    public Size ChunkSize => Size.MB;

    public ulong ChunkCount => (ulong)_fileData.Chunks.Length;
    public ulong GetOffset(ulong chunkIndex)
    {
        return _chunksSorted[chunkIndex].Offset;
    }

    public int GetChunkSize(ulong chunkIndex)
    {
        return (int)_chunksSorted[chunkIndex].UncompressedSize.Value;
    }

    public async Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default)
    {
        await _session._pipeline.ExecuteAsync(async token =>
            {
                var chunk = _chunksSorted[chunkIndex];
                var chunkData = new DepotManifest.ChunkData(chunk.ChunkId.ToArray(), chunk.Checksum.Value, chunk.Offset,
                    (uint)chunk.CompressedSize.Value, (uint)chunk.UncompressedSize.Value
                );
                var server = await _session.CDNPool.GetServer();
                var depotKey = await _session.GetDepotKey(_appId, _depotId);
                string? cdnAuthToken = null;

                var rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
                var read = 0;
                try
                {
                    if (server.Type == "CDN")
                        cdnAuthToken = await _session.CDNPool.GetCDNAuthTokenAsync(_appId, _depotId, server);

                    read = await _session.CDNClient.DownloadDepotChunkAsync(_depotId.Value, chunkData, server,
                        rented, depotKey, cdnAuthToken: cdnAuthToken
                    );
                }
                catch (Exception)
                {
                    _session.CDNPool.FailServer();
                    throw;
                }

                if (read != chunkData.UncompressedLength)
                    throw new InvalidOperationException("Failed to read the entire chunk");
                rented.AsMemory(0, buffer.Length).CopyTo(buffer);
            }, token
        );
    }

    public void ReadChunk(Span<byte> buffer, ulong chunkIndex)
    {
        throw new NotSupportedException();
    }
}
