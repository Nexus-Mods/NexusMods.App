// Modified source, originally:  
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace NexusMods.Paths.HighPerformance.Backports.System.Text.Unicode;

[ExcludeFromCodeCoverage(Justification = "Taken from .NET Runtime")]
internal static class Utf16Utility
{
    /// <summary>
    /// Returns true iff the Vector128 represents 8 ASCII UTF-16 characters in machine endianness.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool AllCharsInVector128AreAscii(Vector128<ushort> vec)
    {
        return (vec & Vector128.Create(unchecked((ushort)~0x007F))) == Vector128<ushort>.Zero;
    }

    /// <summary>
    /// Returns true iff the Vector256 represents 16 ASCII UTF-16 characters in machine endianness.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool AllCharsInVector256AreAscii(Vector256<ushort> vec)
    {
        return (vec & Vector256.Create(unchecked((ushort)~0x007F))) == Vector256<ushort>.Zero;
    }

    /// <summary>
    /// Convert Vector128 that represent 8 ASCII UTF-16 characters to lowercase
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<ushort> Vector128AsciiToLowercase(Vector128<ushort> vec)
    {
        // ASSUMPTION: Caller has validated that input values are ASCII.
        Debug.Assert(AllCharsInVector128AreAscii(vec));

        // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'A'
        var lowIndicator1 = Vector128.Create((sbyte)(0x80 - 'A')) + vec.AsSByte();

        // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'A' and <= 'Z'
        var combIndicator1 = Vector128.LessThan(
            Vector128.Create(unchecked((sbyte)(('Z' - 'A') - 0x80))), lowIndicator1);

        // Add the lowercase indicator (0x20 bit) to all A-Z letters
        return Vector128.AndNot(Vector128.Create((sbyte)0x20), combIndicator1).AsUInt16() + vec;
    }

    /// <summary>
    /// Convert Vector256 that represent 16 ASCII UTF-16 characters to lowercase
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<ushort> Vector256AsciiToLowercase(Vector256<ushort> vec)
    {
        // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'A'
        var lowIndicator1 = Vector256.Create((sbyte)(0x80 - 'A')) + vec.AsSByte();

        // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'A' and <= 'Z'
        var combIndicator1 = Vector256.LessThan(
            Vector256.Create(unchecked((sbyte)(('Z' - 'A') - 0x80))), lowIndicator1);

        // Add the lowercase indicator (0x20 bit) to all A-Z letters
        return Vector256.AndNot(Vector256.Create((sbyte)0x20), combIndicator1).AsUInt16() + vec;
    }

    /// <summary>
    /// Convert Vector128 that represent 8 ASCII UTF-16 characters to uppercase
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<ushort> Vector128AsciiToUppercase(Vector128<ushort> vec)
    {
        // ASSUMPTION: Caller has validated that input values are ASCII.
        Debug.Assert(AllCharsInVector128AreAscii(vec));

        // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'a'
        var lowIndicator1 = Vector128.Create((sbyte)(0x80 - 'a')) + vec.AsSByte();

        // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
        var combIndicator1 = Vector128.LessThan(
            Vector128.Create(unchecked((sbyte)(('z' - 'a') - 0x80))), lowIndicator1);

        // Drop the lowercase indicator (0x20 bit) from all a-z letters
        return vec - Vector128.AndNot(Vector128.Create((sbyte)0x20), combIndicator1).AsUInt16();
    }

    /// <summary>
    /// Convert Vector256 that represent 16 ASCII UTF-16 characters to uppercase
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<ushort> Vector256AsciiToUppercase(Vector256<ushort> vec)
    {
        // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'a'
        var lowIndicator1 = Vector256.Create((sbyte)(0x80 - 'a')) + vec.AsSByte();

        // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
        var combIndicator1 = Vector256.LessThan(
            Vector256.Create(unchecked((sbyte)(('z' - 'a') - 0x80))), lowIndicator1);

        // Drop the lowercase indicator (0x20 bit) from all a-z letters
        return vec - Vector256.AndNot(Vector256.Create((sbyte)0x20), combIndicator1).AsUInt16();
    }
}
