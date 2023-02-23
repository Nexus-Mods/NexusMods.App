using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Common;

/// <summary>
/// string encoding routines
/// </summary>
public static class StringEncodingExtension
{
    /// <summary>
    /// convert string to base 64 encoding
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [Pure]
    public static string ToBase64(this string input)
    {
        return ToBase64(Encoding.UTF8.GetBytes(input));
    }

    /// <summary>
    /// convert byte  array to base 64 encoding
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [Pure]
    public static string ToBase64(this byte[] input)
    {
        return Convert.ToBase64String(input);
    }
}
