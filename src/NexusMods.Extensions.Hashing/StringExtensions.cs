using System.IO.Hashing;
using Reloaded.Memory.Extensions;

namespace NexusMods.Extensions.Hashing;

/// <summary>
///     Extensions revolved around hashing
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Returns the 'stable' hash of a string, made with XxHash64 over
    ///     the raw string bytes.
    ///
    ///     Changing the implementation of this method constitutes a breaking change.
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>The hash of the source string.</returns>
    /// <remarks>
    ///     Instances of <see cref="String"/> will also internally store a null terminator,
    ///     however that's not guaranteed.
    /// </remarks>
    public static ulong GetStableHash(this ReadOnlySpan<char> input)
    {
        return XxHash3.HashToUInt64(input.CastFast<char, byte>());  
    }
    
}
