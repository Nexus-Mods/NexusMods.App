using System.IO.Hashing;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public class Xx3Hasher : IStreamingHasher<Hash, XxHash3, Xx3Hasher>
{
    public static Hash Hash(ReadOnlySpan<byte> input) => Hashing.xxHash3.Hash.From(XxHash3.HashToUInt64(input));

    public static XxHash3 Initialize() => new();

    public static XxHash3 Update(XxHash3 state, ReadOnlySpan<byte> input)
    {
        state.Append(input);
        return state;
    }

    public static Hash Finish(XxHash3 state) => Hashing.xxHash3.Hash.From(state.GetCurrentHashAsUInt64());

    public static ValueTask<Hash> HashAsync(Stream stream, int bufferSize = IStreamingHasher<Hash, XxHash3, Xx3Hasher>.DefaultBufferSize, CancellationToken cancellationToken = default)
    {
        return StreamingHasher<Hash, XxHash3, Xx3Hasher>.HashAsync(stream, bufferSize, cancellationToken);
    }
}
