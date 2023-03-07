namespace NexusMods.Hashing.xxHash64;

public static class ByteArrayExtensions
{
    public static Hash XxHash64(this ReadOnlySpan<byte> data)
    {
        var algo = new xxHashAlgorithm(0);
        return Hash.From(algo.HashBytes(data));
    }

    public static Hash XxHash64(this Memory<byte> data)
    {
        var algo = new xxHashAlgorithm(0);
        return Hash.From(algo.HashBytes(data.Span));
    }
}
