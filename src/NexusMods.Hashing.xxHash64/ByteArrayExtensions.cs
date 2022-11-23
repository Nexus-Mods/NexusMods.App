using NexusMods.DataModel.RateLimiting;

namespace NexusMods.Hashing.xxHash64;

public static class ByteArrayExtensions
{
    public static Hash XxHash64(this ReadOnlySpan<byte> data)
    {
        var algo = new xxHashAlgorithm(0);
        return new Hash(algo.HashBytes(data));
    }
}