using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace NexusMods.Extensions.BCL;

/// <summary>
/// String encoding routines
/// </summary>
[PublicAPI]
public static class StringBase64Extensions
{
    /// <summary>
    /// Convert string to base 64 encoding
    /// </summary>
    public static string ToBase64(this string input)
    {
        return ToBase64(Encoding.UTF8.GetBytes(input));
    }

    /// <summary>
    /// Convert byte array to base 64 encoding
    /// </summary>
    public static string ToBase64(this byte[] input)
    {
        return Convert.ToBase64String(input);
    }

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
    [SkipLocalsInit]
    public static string Base64UrlEncode(ReadOnlySpan<byte> input)
    {
        // TODO: use Microsoft.AspNetCore.WebUtilities when .NET 8 is available
        // Source: https://github.com/dotnet/aspnetcore/blob/main/src/Shared/WebEncoders/WebEncoders.cs
        // The MIT License (MIT)
        //
        // Copyright (c) .NET Foundation and Contributors
        //
        // All rights reserved.
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.

        const int stackAllocThreshold = 128;

        if (input.IsEmpty)
        {
            return string.Empty;
        }

        var bufferSize = GetArraySizeRequiredToEncode(input.Length);

        char[]? bufferToReturnToPool = null;
        var buffer = bufferSize <= stackAllocThreshold
            ? stackalloc char[stackAllocThreshold]
            : bufferToReturnToPool = ArrayPool<char>.Shared.Rent(bufferSize);

        var numBase64Chars = Base64UrlEncode(input, buffer);
        var base64Url = new string(buffer[..numBase64Chars]);

        if (bufferToReturnToPool != null)
        {
            ArrayPool<char>.Shared.Return(bufferToReturnToPool);
        }

        return base64Url;
    }


    private static int Base64UrlEncode(ReadOnlySpan<byte> input, Span<char> output)
    {
        Debug.Assert(output.Length >= GetArraySizeRequiredToEncode(input.Length));

        if (input.IsEmpty)
        {
            return 0;
        }

        // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

        Convert.TryToBase64Chars(input, output, out int charsWritten);

        // Fix up '+' -> '-' and '/' -> '_'. Drop padding characters.
        for (var i = 0; i < charsWritten; i++)
        {
            var ch = output[i];
            switch (ch)
            {
                case '+':
                    output[i] = '-';
                    break;
                case '/':
                    output[i] = '_';
                    break;
                case '=':
                    // We've reached a padding character; truncate the remainder.
                    return i;
            }
        }

        return charsWritten;
    }

    private static int GetArraySizeRequiredToEncode(int count)
    {
        var numWholeOrPartialInputBlocks = checked(count + 2) / 3;
        return checked(numWholeOrPartialInputBlocks * 4);
    }
}
