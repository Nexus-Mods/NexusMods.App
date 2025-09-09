using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public static class MultiHasher
{
    private const int BufferSize = 64 * 1024;
    private const int MaxFullFileHash = BufferSize * 2;

    public static async Task<MultiHash> HashStream(Stream stream, CancellationToken cancellationToken = default)
    {
        stream.Position = 0;
        var buffer = GC.AllocateUninitializedArray<byte>(BufferSize);

        var md5 = MD5.Create();
        var xxHash3 = new XxHash3();
        var xxHash64 = new XxHash64();
        var crc32 = new Crc32();
        var sha1 = SHA1.Create();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;

            var span = buffer.AsSpan(0, bytesRead);
            xxHash3.Append(span);
            xxHash64.Append(span);
            crc32.Append(span);
            md5.TransformBlock(buffer, inputOffset: 0, inputCount: bytesRead, buffer, outputOffset: 0);
            sha1.TransformBlock(buffer, inputOffset: 0, inputCount: bytesRead, buffer, outputOffset: 0);
        }

        md5.TransformFinalBlock(buffer, inputOffset: 0, inputCount: 0);
        sha1.TransformFinalBlock(buffer, inputOffset: 0, inputCount: 0);

        var minimalHash = await MinimalHash(stream, cancellationToken: cancellationToken);

        var result = new MultiHash
        {
            XxHash3 = Hash.From(xxHash3.GetCurrentHashAsUInt64()),
            XxHash64 = Hash.From(xxHash64.GetCurrentHashAsUInt64()),
            MinimalHash = minimalHash,
            Sha1 = Sha1Value.From(sha1.Hash),
            Md5 = Md5Value.From(md5.Hash),
            Crc32 = Crc32Value.From(crc32.GetCurrentHashAsUInt32()),
            Size = Size.FromLong(stream.Length),
        };

        return result;
    }

    public static async ValueTask<Hash> MinimalHash(Stream stream, CancellationToken cancellationToken = default)
    {
        stream.Position = 0;
        var hasher = new XxHash3();

        if (stream.Length <= MaxFullFileHash)
        {
            await hasher.AppendAsync(stream, cancellationToken);
            return Hash.From(hasher.GetCurrentHashAsUInt64());
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

        return Hash.From(hasher.GetCurrentHashAsUInt64());
    }
}
