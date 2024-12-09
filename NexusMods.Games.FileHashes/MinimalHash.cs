using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Games.FileHashes;

public static class MinimalHashExtensions
{
    private const int BufferSize = 64 * 1024;
    private const int MaxFullFileHash = BufferSize * 2;

    /// <summary>
    /// Perform a minimal hash of a file, reading only the start, middle, and end of the file.
    /// </summary>
    public static async Task<Hash> MinimalHash(this Stream stream, CancellationToken cancellationToken = default)
    {
        var hashAlgo = new XxHash3();
        
        stream.Position = 0;
        if (stream.Length <= MaxFullFileHash)
            return await stream.xxHash3Async(cancellationToken);
        
        using var bufferOwner = MemoryPool<byte>.Shared.Rent(BufferSize);
        var buffer = bufferOwner.Memory[..BufferSize];
        
        // Read the block at the start of the file
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hashAlgo.Append(buffer.Span);
        
        // Read the block at the end of the file
        stream.Position = stream.Length - BufferSize;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hashAlgo.Append(buffer.Span);
        
        // Read the block in the middle, if the file is too small, offset the middle enough to not read past the end
        // of the file
        var middleOffset = Math.Min(stream.Length / 2, stream.Length - BufferSize);
        stream.Position = middleOffset;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hashAlgo.Append(buffer.Span);
        
        // Add the length of the file to the hash (as a ulong)
        Span<byte> lengthBuffer = stackalloc byte[sizeof(long)];
        MemoryMarshal.Write(lengthBuffer, (ulong)stream.Length);
        hashAlgo.Append(lengthBuffer);

        // Return the hash
        return Hash.From(hashAlgo.GetCurrentHashAsUInt64());
    }
}
