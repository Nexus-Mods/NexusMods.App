namespace NexusMods.Hashing.xxHash64;

/// <summary>
/// Extensions related to <see cref="Span{T}"/>(s) and their heap sibling <see cref="Memory{T}"/>.
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    /// Hashes the given <see cref="Span{T}"/> of bytes using xxHash64.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>Hash for the given data.</returns>
    public static Hash XxHash64(this Span<byte> data) => XxHash64((ReadOnlySpan<byte>)data);

    /// <summary>
    /// Hashes the given <see cref="Span{T}"/> of bytes using xxHash64.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>Hash for the given data.</returns>
    public static Hash XxHash64(this ReadOnlySpan<byte> data)
    {
        var algo = new XxHash64Algorithm(0);
        return Hash.From(algo.HashBytes(data));
    }

    /// <summary>
    /// Hashes the given <see cref="Memory{T}"/> of bytes using xxHash64.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>Hash for the given data.</returns>
    public static Hash XxHash64(this Memory<byte> data) => XxHash64(data.Span);
}
