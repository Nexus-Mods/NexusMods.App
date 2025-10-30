using System.IO.Hashing;
using System.Runtime.InteropServices;
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

        var crcState = Crc32Hasher.Initialize();
        var md5State = Md5Hasher.Initialize();
        var sha1State = Sha1Hasher.Initialize();
        var xxHash3State = Xx3Hasher.Initialize();
        var xxHash64State = Xx64Hasher.Initialize();

        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;

            crcState = Crc32Hasher.Update(crcState, buffer);
            md5State = Md5Hasher.Update(md5State, buffer);
            sha1State = Sha1Hasher.Update(sha1State, buffer);
            xxHash3State = Xx3Hasher.Update(xxHash3State, buffer);
            xxHash64State = Xx64Hasher.Update(xxHash64State, buffer);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var minimalHash = await MinimalHash<Hash, XxHash3, Xx3Hasher>(stream, cancellationToken: cancellationToken);

        var result = new MultiHash
        {
            Crc32 = Crc32Hasher.Finish(crcState),
            Md5 = Md5Hasher.Finish(md5State),
            Sha1 = Sha1Hasher.Finish(sha1State),
            XxHash3 = Xx3Hasher.Finish(xxHash3State),
            XxHash64 = Xx64Hasher.Finish(xxHash64State),

            MinimalHash = minimalHash,
            Size = Size.FromLong(stream.Length),
        };

        return result;
    }

    public static async ValueTask<THash> MinimalHash<THash, TState, THasher>(Stream stream, CancellationToken cancellationToken = default)
        where THash : unmanaged, IEquatable<THash>
        where THasher : IStreamingHasher<THash, TState, THasher>
    {
        stream.Position = 0;
        if (stream.Length <= MaxFullFileHash) return await THasher.HashAsync(stream, cancellationToken: cancellationToken);

        var buffer = GC.AllocateUninitializedArray<byte>(BufferSize);
        var state = THasher.Initialize();

        // Read the block at the start of the file
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        state = THasher.Update(state, buffer);

        // Read the block at the end of the file
        stream.Position = stream.Length - BufferSize;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        state = THasher.Update(state, buffer);

        // Read the block in the middle, if the file is too small, offset the middle enough to not read past the end
        // of the file
        var middleOffset = Math.Min(stream.Length / 2, stream.Length - BufferSize);
        stream.Position = middleOffset;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        state = THasher.Update(state, buffer);

        // Add the length of the file to the hash (as an ulong)
        Span<byte> lengthBuffer = stackalloc byte[sizeof(ulong)];
        MemoryMarshal.Write(lengthBuffer, (ulong)stream.Length);
        state = THasher.Update(state, lengthBuffer);

        return THasher.Finish(state);
    }
}
