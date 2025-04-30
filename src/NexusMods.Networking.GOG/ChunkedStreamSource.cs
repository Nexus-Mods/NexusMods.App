using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.IO.ChunkedStreams;
using NexusMods.Paths;
using SmartFormat;

namespace NexusMods.Networking.GOG;

/// <summary>
/// Chunked stream loader for GOG data
/// </summary>
internal class ChunkedStreamSource : IChunkedStreamSource
{
    private readonly Client _client;
    private readonly Chunk[] _chunks;
    private readonly ulong[] _offsets;
    private readonly SecureUrl _secureUrl;
    private readonly bool _putInCache;

    public ChunkedStreamSource(Client client, Chunk[] chunks, Size size, SecureUrl url, bool putInCache = false)
    {
        _secureUrl = url;
        _client = client;
        _chunks = chunks;
        _putInCache = putInCache;
        
        _offsets = new ulong[_chunks.Length];
        ulong offset = 0;
        for (var i = 0; i < _chunks.Length; i++)
        {
            _offsets[i] = offset;
            offset += (ulong)_chunks[i].Size;
        }
        
        Size = size;
    }
    
    public Size Size { get; }

    public ulong ChunkCount => (ulong)_chunks.Length;
    public ulong GetOffset(ulong chunkIndex)
    {
        return _offsets[chunkIndex];
    }

    public int GetChunkSize(ulong chunkIndex)
    {
        return (int)_chunks[chunkIndex].Size.Value;
    }

    public async Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default)
    {
        if (chunkIndex >= (ulong)_chunks.Length)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));

        var chunk = _chunks[chunkIndex];
        if (_client.TryGetCachedBlock(chunk.CompressedMd5, out var blockData))
        {
            blockData.Span.CopyTo(buffer.Span);
            return;
        }
        
        await _client._pipeline.ExecuteAsync(async token =>
            {
                var parameters = _secureUrl.Parameters;
                var md5s = chunk.CompressedMd5.ToString().ToLower();
                parameters = parameters with { path = parameters.path + $"/{md5s[..2]}/{md5s[2..4]}/{md5s}" };
                var url = Smart.Format(_secureUrl.UrlFormat, parameters);

                await using var chunkStream = await _client.HttpClient.GetStreamAsync(url, token);
                await using var zlibStream = new ZLibStream(chunkStream, CompressionMode.Decompress);

                await zlibStream.ReadExactlyAsync(buffer, token);
            }
        );

        #if DEBUG
        var md5 = Md5.From(MD5.HashData(buffer.Span));
        Debug.Assert(md5.Equals(chunk.Md5));
        #endif
        
        if (_putInCache) 
            _client.AddCachedBlock(chunk.CompressedMd5, buffer.ToArray());
    }
    

    public void ReadChunk(Span<byte> buffer, ulong chunkIndex)
    {
        throw new NotSupportedException();
    }
}
