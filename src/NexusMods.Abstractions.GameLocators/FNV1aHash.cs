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
            hash ^= data[i];
            hash *= FNV1aPrime;
        }
        return hash;
    }
    
}
