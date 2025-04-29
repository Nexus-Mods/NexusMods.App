using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Hashes;

/// <summary>
/// Hashes to all the common hash algorithms.
/// </summary>
public class MultiHasher
{
    private readonly MD5 _md5;
    private readonly XxHash3 _xxHash3;
    private readonly System.IO.Hashing.Crc32 _crc32;
    private readonly SHA1 _sha1;
    private readonly byte[] _buffer;
    private readonly XxHash3 _minimalHash;
    private readonly XxHash64 _xxHash64;

    private const int BufferSize = 64 * 1024;
    private const int MaxFullFileHash = BufferSize * 2;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public MultiHasher()
    {
        _md5 = MD5.Create();
        _xxHash3 = new XxHash3();
        _xxHash64 = new XxHash64();
        _minimalHash = new XxHash3();
        _crc32 = new System.IO.Hashing.Crc32();
        _sha1 = SHA1.Create();
        _buffer = new byte[1024 * 128];
    }

    /// <summary>
    /// Hashes the stream to all the common hash algorithms.
    /// </summary>
    public async Task<MultiHash> HashStream(Stream stream, CancellationToken token = default, Func<Size, ValueTask>? onSize = null)
    {
        stream.Position = 0;

        var lastUpdate = Size.Zero;
        var totalRead = Size.Zero;
        var updateInterval = Size.FromLong(1024 * 1024 * 10);
        
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var read = await stream.ReadAsync(_buffer, token);
            totalRead += Size.FromLong(read);

            if (onSize != null && lastUpdate + updateInterval < totalRead)
            {
                await onSize.Invoke(totalRead - lastUpdate);
                lastUpdate = totalRead;
            }

            if (read == 0)
                break;
            var span = _buffer.AsSpan(0, read);
            _xxHash3.Append(span);
            _xxHash64.Append(span);
            _crc32.Append(span);
            _md5.TransformBlock(_buffer, 0, read, _buffer, 0);
            _sha1.TransformBlock(_buffer, 0, read, _buffer, 0);
        }
        
        _md5.TransformFinalBlock(_buffer, 0, 0);
        _sha1.TransformFinalBlock(_buffer, 0, 0);
        await MinimalHash(_minimalHash, stream, token);

        var result = new MultiHash
        {
            XxHash3 = Hash.From(_xxHash3.GetCurrentHashAsUInt64()),
            XxHash64 = Hash.From(_xxHash64.GetCurrentHashAsUInt64()),
            MinimalHash = Hash.From(_minimalHash.GetCurrentHashAsUInt64()),
            Sha1 = Sha1.From(_sha1.Hash),
            Md5 = Md5.From(_md5.Hash),
            Size = Size.FromLong(stream.Length),
            Crc32 = Crc32.From(_crc32.GetCurrentHashAsUInt32()),
        };
        

        return result;
    }


    /// <summary>
    /// Hash a file with the minimal hash algorithm.
    /// </summary>
    public static async Task<Hash> MinimalHash(AbsolutePath path, CancellationToken token = default)
    {
        var hasher = new XxHash3();
        await using var stream = path.Read();
        await MinimalHash(hasher, stream, token);
        return Hash.From(hasher.GetCurrentHashAsUInt64());
    }
    
    /// <summary>
    /// Calculates a minimal hash of the stream.
    /// </summary>
    public static async Task MinimalHash(XxHash3 hasher, Stream stream, CancellationToken cancellationToken = default)
    {
        stream.Position = 0;
        if (stream.Length <= MaxFullFileHash)
        {
            await hasher.AppendAsync(stream, cancellationToken);
            return;
        }

        using var bufferOwner = MemoryPool<byte>.Shared.Rent(BufferSize);
        var buffer = bufferOwner.Memory[..BufferSize];

        // Read the block at the start of the file
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hasher.Append(buffer.Span);

        // Read the block at the end of the file
        stream.Position = stream.Length - BufferSize;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hasher.Append(buffer.Span);

        // Read the block in the middle, if the file is too small, offset the middle enough to not read past the end
        // of the file
        var middleOffset = Math.Min(stream.Length / 2, stream.Length - BufferSize);
        stream.Position = middleOffset;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hasher.Append(buffer.Span);

        // Add the length of the file to the hash (as an ulong)
        Span<byte> lengthBuffer = stackalloc byte[sizeof(ulong)];
        MemoryMarshal.Write(lengthBuffer, (ulong)stream.Length);
        hasher.Append(lengthBuffer);
    }
    
}
