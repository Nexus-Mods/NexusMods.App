using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using NexusMods.Paths.HighPerformance.Backports.System.Globalization;

namespace NexusMods.Paths.Extensions;

/// <summary>
/// Path related extensions tied to strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Normalizes the given string in-place for use with string comparisons across
    /// the library such that they are case insensitive and use a consistent separator.
    /// </summary>
    public static bool CompareStringsCaseAndSeparatorInsensitive(string a, string b)
    {
        Span<char> aCopy = a.Length <= 512 ? stackalloc char[a.Length] : GC.AllocateUninitializedArray<char>(a.Length);
        Span<char> bCopy = b.Length <= 512 ? stackalloc char[b.Length] : GC.AllocateUninitializedArray<char>(b.Length);
        a.CopyTo(aCopy);
        b.CopyTo(bCopy);

        NormalizeStringCaseAndPathSeparator(aCopy);
        NormalizeStringCaseAndPathSeparator(bCopy);

        // Strings are normalized, so we can do fast ordinal compare.
        return aCopy.SequenceEqual(bCopy);
    }

    /// <summary>
    /// Normalizes the given string in-place for use with string comparisons across
    /// the library such that they are case insensitive and use a consistent separator.
    /// </summary>
    public static void NormalizeStringCaseAndPathSeparator(this Span<char> text)
    {
        TextInfo.ChangeCase<TextInfo.ToLowerConversion>(text, text);
        text.Replace('\\', '/', text);
    }

    #region Legacy API

    /// <summary>
    /// Converts an existing path represented as a string to a <see cref="RelativePath"/>.
    /// </summary>
    public static RelativePath ToRelativePath(this string s) => (RelativePath)s;

    /// <summary>
    /// Converts an existing path represented as a string to a <see cref="AbsolutePath"/>.
    /// </summary>
    public static AbsolutePath ToAbsolutePath(this string s) => (AbsolutePath)s;

    #endregion

    /// <summary>
    /// Faster hashcode for strings; but does not randomize between application runs.
    /// Inspired by .NET Runtime's own implementation; combining unrolled djb-like and FNV-1.
    /// </summary>
    /// <param name="text">The string for which to get hash code for.</param>
    /// <remarks>
    ///     Use this if and only if 'Denial of Service' attacks are not a concern (i.e. never used for free-form user input),
    ///     or are otherwise mitigated.
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification = "Cannot be accurately measured without multiple architectures.")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe int GetNonRandomizedHashCode32(this ReadOnlySpan<char> text)
    {
        return GetNonRandomizedHashCode(text).GetHashCode();
    }

/// <summary>
    /// Faster hashcode for strings; but does not randomize between application runs.
    /// Inspired by .NET Runtime's own implementation; combining unrolled djb-like and FNV-1.
    /// </summary>
    /// <param name="text">The string for which to get hash code for.</param>
    /// <remarks>
    ///     Use this if and only if 'Denial of Service' attacks are not a concern (i.e. never used for free-form user input),
    ///     or are otherwise mitigated.
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification = "Cannot be accurately measured without multiple architectures.")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe nuint GetNonRandomizedHashCode(this ReadOnlySpan<char> text)
    {
        // Sewer: This is from my VFS.
        fixed (char* src = &text.GetPinnableReference())
        {
            int length = text.Length; // Span has no guarantee of null terminator.
            
            // For short strings below size of nuint, we need separate approach; so we use legacy runtime approach
            // for said cold case.
            if (length >= sizeof(nuint) / sizeof(char))
            {
                nuint hash1 = (5381 << 16) + 5381;
                nuint hash2 = hash1;

                // I tried aligning the data here; but it didn't help much perf wise
                // despite being 3-4 instructions. I do not know why.
                nuint* ptr = (nuint*)(src);
                
                // Note. In this implementations we leave some (< sizeof(nuint)) data from the hash.
                
                // For our use of hashing file paths, this is okay, as files with different names but same extension
                // would still hash differently. If I were to PR this to runtime though, this would need fixing.
                
                if (Avx2.IsSupported || Vector128.IsHardwareAccelerated)
                {
                    // AVX Version
                    // Ideally I could rewrite this in full Vector256 but I don't know how to get it to emit VPMULUDQ for the multiply operation.
                    if (Avx2.IsSupported && length >= sizeof(Vector256<ulong>) / sizeof(char) * 4) // over 128 bytes + AVX
                    {
                        var prime = Vector256.Create((ulong)0x100000001b3);
                        var hash1Avx = Vector256.Create(0xcbf29ce484222325);
                        var hash2Avx = Vector256.Create(0xcbf29ce484222325);
                        
                        while (length >= sizeof(Vector256<ulong>) / sizeof(char) * 4) // 128 byte chunks.
                        {
                            length -= (sizeof(Vector256<ulong>) / sizeof(char)) * 4;
                            hash1Avx = Avx2.Xor(hash1Avx, Avx.LoadVector256((ulong*)ptr));
                            hash1Avx = Avx2.Multiply(hash1Avx.AsUInt32(), prime.AsUInt32());
                            
                            hash2Avx = Avx2.Xor(hash2Avx, Avx.LoadVector256((ulong*)ptr + 4));
                            hash2Avx = Avx2.Multiply(hash2Avx.AsUInt32(), prime.AsUInt32());

                            hash1Avx = Avx2.Xor(hash1Avx, Avx.LoadVector256((ulong*)ptr + 8));
                            hash1Avx = Avx2.Multiply(hash1Avx.AsUInt32(), prime.AsUInt32());

                            hash2Avx = Avx2.Xor(hash2Avx, Avx.LoadVector256((ulong*)ptr + 12));
                            hash2Avx = Avx2.Multiply(hash2Avx.AsUInt32(), prime.AsUInt32());
                            ptr += (sizeof(Vector256<ulong>) / sizeof(nuint)) * 4;
                        }
                        
                        while (length >= sizeof(Vector256<ulong>) / sizeof(char)) // 32 byte chunks.
                        {
                            length -= sizeof(Vector256<ulong>) / sizeof(char);
                            hash1Avx = Avx2.Xor(hash1Avx, Avx.LoadVector256((ulong*)ptr));
                            hash1Avx = Avx2.Multiply(hash1Avx.AsUInt32(), prime.AsUInt32());
                            ptr += (sizeof(Vector256<ulong>) / sizeof(nuint));
                        }
                        
                        // Flatten
                        hash1Avx ^= hash2Avx;
                        if (sizeof(nuint) == 8)
                        {
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (nuint)hash1Avx[0];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (nuint)hash1Avx[1];
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (nuint)hash1Avx[2];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (nuint)hash1Avx[3];
                        }
                        else
                        {
                            var hash1Uint = hash1Avx.AsUInt32();
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (hash1Uint[0] * hash1Uint[1]);
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (hash1Uint[2] * hash1Uint[3]);
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (hash1Uint[3] * hash1Uint[4]);
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (hash1Uint[5] * hash1Uint[6]);
                        }
                        
                        // 4/8 byte remainders
                        while (length >= (sizeof(nuint) / sizeof(char)))
                        {
                            length -= (sizeof(nuint) / sizeof(char));
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                            ptr += 1;
                        }
                        
                        return hash1 + (hash2 * 1566083941);
                    }

                    // Over 64 bytes + SSE. Supported on all x64 processors
                    if (Vector128.IsHardwareAccelerated && length >= sizeof(Vector128<ulong>) / sizeof(char) * 4) 
                    {
                        var prime = Vector128.Create((ulong)0x100000001b3);
                        var hash1_128 = Vector128.Create(0xcbf29ce484222325);
                        var hash2_128 = Vector128.Create(0xcbf29ce484222325);

                        while (length >= sizeof(Vector128<ulong>) / sizeof(char) * 4) // 64 byte chunks.
                        {
                            length -= (sizeof(Vector128<ulong>) / sizeof(char)) * 4;
                            hash1_128 = Vector128.Xor(hash1_128, Vector128.Load((ulong*)ptr));
                            hash1_128 = Vector128.Multiply(hash1_128.AsUInt32(), prime.AsUInt32()).AsUInt64();

                            hash2_128 = Vector128.Xor(hash2_128, Vector128.Load((ulong*)ptr + 2));
                            hash2_128 = Vector128.Multiply(hash2_128.AsUInt32(), prime.AsUInt32()).AsUInt64();

                            hash1_128 = Vector128.Xor(hash1_128, Vector128.Load((ulong*)ptr + 4));
                            hash1_128 = Vector128.Multiply(hash1_128.AsUInt32(), prime.AsUInt32()).AsUInt64();

                            hash2_128 = Vector128.Xor(hash2_128, Vector128.Load((ulong*)ptr + 6));
                            hash2_128 = Vector128.Multiply(hash2_128.AsUInt32(), prime.AsUInt32()).AsUInt64();
                            ptr += (sizeof(Vector128<ulong>) / sizeof(nuint)) * 4;
                        }
                        
                        while (length >= sizeof(Vector128<ulong>) / sizeof(char)) // 16 byte chunks.
                        {
                            length -= sizeof(Vector128<ulong>) / sizeof(char);
                            hash1_128 = Vector128.Xor(hash1_128, Vector128.Load((ulong*)ptr));
                            hash1_128 = Vector128.Multiply(hash1_128.AsUInt32(), prime.AsUInt32()).AsUInt64();
                            ptr += (sizeof(Vector128<ulong>) / sizeof(nuint));
                        }
                        
                        // Flatten
                        hash1_128 ^= hash2_128;
                        if (sizeof(nuint) == 8)
                        {
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (nuint)hash1_128[0];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (nuint)hash1_128[1];
                        }
                        else
                        {
                            var hash1Uint = hash1_128.AsUInt32();
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (hash1Uint[0] * hash1Uint[1]);
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (hash1Uint[2] * hash1Uint[3]);
                        }
                        
                        // 4/8 byte remainders
                        while (length >= (sizeof(nuint) / sizeof(char)))
                        {
                            length -= (sizeof(nuint) / sizeof(char));
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                            ptr += 1;
                        }
                        
                        return hash1 + (hash2 * 1566083941);
                    }

                    if (sizeof(nuint) == 8) // 64-bit. Max 8 operations. (8 * 8 = 64bytes)
                    {
                        // 16 byte loop
                        while (length >= (sizeof(nuint) / sizeof(char)) * 2)
                        {
                            length -= (sizeof(nuint) / sizeof(char)) * 2;
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                            ptr += 2;
                        }
                        
                        if (length >= sizeof(nuint) / sizeof(char))
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                        
                        return hash1 + (hash2 * 1566083941);
                    }
                    else if (sizeof(nuint) == 4) // 32-bit. Max 16 operations (16 * 4 = 64 bytes)
                    {
                        // 16 byte loop
                        while (length >= (sizeof(nuint) / sizeof(char)) * 4)
                        {
                            length -= (sizeof(nuint) / sizeof(char)) * 4;
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[2];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[3];
                            ptr += 4;
                        }

                        // 8 byte
                        if (length >= (sizeof(nuint) / sizeof(char)) * 2)
                        {
                            length -= (sizeof(nuint) / sizeof(char)) * 2;
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                            ptr += 2;
                        }

                        // 4 byte
                        if (length >= (sizeof(nuint) / sizeof(char)))
                            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                        
                        return  hash1 + (hash2 * 1566083941);
                    }
                    
                    // The future is now.
                    return NonRandomizedHashCode_Fallback(src, length);
                }

                // Non-vector accelerated version here.
                // 32/64 byte loop
                while (length >= (sizeof(nuint) / sizeof(char)) * 8)
                {
                    length -= (sizeof(nuint) / sizeof(char)) * 8;
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[2];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[3];
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[4];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[5];
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[6];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[7];
                    ptr += 8;
                }

                // 16/32 byte
                if (length >= (sizeof(nuint) / sizeof(char)) * 4)
                {
                    length -= (sizeof(nuint) / sizeof(char)) * 4;
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[2];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[3];
                    ptr += 4;
                }

                // 8/16 byte
                if (length >= (sizeof(nuint) / sizeof(char)) * 2)
                {
                    length -= (sizeof(nuint) / sizeof(char)) * 2;
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    ptr += 2;
                }

                // 4/8 byte
                if (length >= (sizeof(nuint) / sizeof(char)))
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];

                return hash1 + (hash2 * 1566083941);
            }

            return NonRandomizedHashCode_Fallback(src, length);
        }
    }
    
    [ExcludeFromCodeCoverage(Justification = "Cannot be accurately measured without multiple architectures.")]
    private static unsafe nuint NonRandomizedHashCode_Fallback(char* src, int length)
    {
        // -1 because we cannot assume string has null terminator at end unlike runtime.
        length -= 1;
        
        // Version for when input data is smaller than native int. This one is taken from the runtime.
        // For tiny strings like 'C:'
        uint hash1 = (5381 << 16) + 5381;
        uint hash2 = hash1;
        uint* ptr = (uint*)src;

        while (length > 2)
        {
            length -= 4;
            // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
            hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
            ptr += 2;
        }

        if (length > 0)
        {
            // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
            hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
        }

        return hash1 + (hash2 * 1566083941);
    }
}