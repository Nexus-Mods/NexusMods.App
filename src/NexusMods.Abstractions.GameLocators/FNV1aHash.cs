namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// A simple FNV1a hash implementation for hashing strings to 32-bit integers
/// </summary>
public static class FNV1aHash
{
    private const uint FNV1aPrime = 0x01000193U;

    private const uint FNV1aOffsetBasis = 0x811C9DC5U;
    
    /// <summary>
    /// Get the FNV1a hash of the given string
    /// </summary>
    public static uint Hash(ReadOnlySpan<char> data)
    {
        var hash = FNV1aOffsetBasis;
        for(var i = 0; i < data.Length; i++)
        {
            var c = data[i];
#if DEBUG
            if (c > 0x7F)
                throw new ArgumentOutOfRangeException(nameof(data), 
                    $"Non-ASCII character detected at position {i}: U+{(int)c:X4}");
#endif
            hash ^= c;
            hash *= FNV1aPrime;
        }
        return hash;
    }
    
    /// <summary>
    /// Get the FNV1a hash of the given string
    /// </summary>
    public static uint Hash(ReadOnlySpan<byte> data)
    {
        var hash = FNV1aOffsetBasis;
        for(var i = 0; i < data.Length; i++)
        {
            var c = data[i];
#if DEBUG
            if (c > 0x7F)
                throw new ArgumentOutOfRangeException(nameof(data), 
                    $"Non-ASCII character detected at position {i}: U+{(int)c:X4}");
#endif
            hash ^= c;
            hash *= FNV1aPrime;
        }
        return hash;
    }

    /// <summary>
    /// Mixes a 32-bit hash into a 16-bit unsigned short by XOR-ing the higher and lower 16 bits.
    /// </summary>
    /// <param name="hash">The 32-bit hash value to mix into a 16-bit unsigned short.</param>
    /// <returns>A 16-bit unsigned short generated from the given 32-bit hash.</returns>
    public static ushort MixToShort(uint hash)
    {
        return (ushort) ((hash >> 16) ^ (hash & 0xFFFF));
    }
    
}
