using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// FNV Non-Cryptographic Hash Algorithm.
/// </summary>
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class FNV1a
{
    // https://datatracker.ietf.org/doc/html/draft-eastlake-fnv-35#name-fnv-constants
    private const uint Prime32 = 0x01000193;
    private const uint Offset32 = 0x811C9DC5;

    public static uint Hash32(ReadOnlySpan<char> input)
    {
        var hash = Offset32;
        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            hash ^= current;
            hash *= Prime32;
        }

        return hash;
    }

    public static uint Hash32(ReadOnlySpan<byte> input)
    {
        var hash = Offset32;
        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            hash ^= current;
            hash *= Prime32;
        }

        return hash;
    }

    public static ushort Hash16(ReadOnlySpan<char> input) => MixToShort(Hash32(input));
    public static ushort Hash16(ReadOnlySpan<byte> input) => MixToShort(Hash32(input));

    /// <summary>
    /// Mixes a 32-bit hash into a 16-bit unsigned short by XOR-ing the higher and lower 16 bits.
    /// </summary>
    private static ushort MixToShort(uint hash)
    {
        return (ushort) ((hash >> 16) ^ (hash & 0xFFFF));
    }
}

/// <summary>
/// String hash pool using 32-bit FNV1a hashes.
/// </summary>
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class FNV1a32Pool : AStringHashPool<uint>
{
    public FNV1a32Pool(string name) : base(name) { }

    protected override uint Hash(string input) => FNV1a.Hash32(input);
}

/// <summary>
/// String hash pool using 16-bit FNV1a hashes.
/// </summary>
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class FNV1a16Pool : AStringHashPool<ushort>
{
    public FNV1a16Pool(string name) : base(name) { }

    protected override ushort Hash(string input) => FNV1a.Hash16(input);
}
