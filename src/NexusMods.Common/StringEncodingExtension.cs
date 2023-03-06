using System.Diagnostics.Contracts;
using System.Text;

namespace NexusMods.Common;

/// <summary>
/// String encoding routines
/// </summary>
public static class StringEncodingExtension
{
    /// <summary>
    /// Convert string to base 64 encoding
    /// </summary>
    [Pure]
    public static string ToBase64(this string input)
    {
        return ToBase64(Encoding.UTF8.GetBytes(input));
    }

    /// <summary>
    /// Convert byte array to base 64 encoding
    /// </summary>
    [Pure]
    public static string ToBase64(this byte[] input)
    {
        return Convert.ToBase64String(input);
    }
}
