using System.IO.Hashing;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public class Xx64Hasher : IStreamingHasher<Hash, XxHash64, Xx64Hasher>
{
    public static Hash Hash(ReadOnlySpan<byte> input) => Hashing.xxHash3.Hash.From(XxHash64.HashToUInt64(input));

    public static XxHash64 Initialize() => new();

    public static XxHash64 Update(XxHash64 state, ReadOnlySpan<byte> input)
    {
        state.Append(input);
        return state;
    }

    public static Hash Finish(XxHash64 state) => Hashing.xxHash3.Hash.From(state.GetCurrentHashAsUInt64());

    public static ValueTask<Hash> HashAsync(Stream stream, int bufferSize = IStreamingHasher<Hash, XxHash64, Xx64Hasher>.DefaultBufferSize, CancellationToken cancellationToken = default)
    {
        return StreamingHasher<Hash, XxHash64, Xx64Hasher>.HashAsync(stream, bufferSize, cancellationToken);
    }
}
