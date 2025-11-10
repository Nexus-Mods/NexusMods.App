using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Represents a hash algorithm.
/// </summary>
[PublicAPI]
public interface IHasher<out THash, TSelf>
    where THash : unmanaged, IEquatable<THash>
    where TSelf : IHasher<THash, TSelf>
{
    /// <summary>
    /// Hashes the input.
    /// </summary>
    static abstract THash Hash(ReadOnlySpan<byte> input);
}

/// <summary>
/// Represents a hash algorithm.
/// </summary>
[PublicAPI]
public interface IStringHasher<out THash, TSelf> : IHasher<THash, TSelf>
    where THash : unmanaged, IEquatable<THash>
    where TSelf : IStringHasher<THash, TSelf>
{
    /// <summary>
    /// Hashes the input.
    /// </summary>
    static abstract THash Hash(ReadOnlySpan<char> input);
}
