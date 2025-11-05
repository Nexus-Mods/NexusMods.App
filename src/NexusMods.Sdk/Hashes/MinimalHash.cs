using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public static class MinimalHash
{
    public static async ValueTask<THash> HashAsync<THash, TState, THasher>(Stream stream, CancellationToken cancellationToken = default)
        where THash : unmanaged, IEquatable<THash>
        where THasher : IStreamingHasher<THash, TState, THasher>
    {
        const int bufferSize = 64 * 1024;
        const int maxFullFileHash = bufferSize * 2;

        stream.Position = 0;
        if (stream.Length <= maxFullFileHash) return await THasher.HashAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var buffer = GC.AllocateUninitializedArray<byte>(bufferSize);
        var state = THasher.Initialize();

        // Read the block at the start of the file
        await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        state = THasher.Update(state, buffer);

        // Read the block at the end of the file
        stream.Position = stream.Length - bufferSize;
        await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        state = THasher.Update(state, buffer);

        // Read the block in the middle, if the file is too small, offset the middle enough to not read past the end
        // of the file
        var middleOffset = Math.Min(stream.Length / 2, stream.Length - bufferSize);
        stream.Position = middleOffset;
        await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        state = THasher.Update(state, buffer);

        // Add the length of the file to the hash (as an ulong)
        Span<byte> lengthBuffer = stackalloc byte[sizeof(ulong)];
        MemoryMarshal.Write(lengthBuffer, (ulong)stream.Length);
        state = THasher.Update(state, lengthBuffer);

        return THasher.Finish(state);
    }
}
