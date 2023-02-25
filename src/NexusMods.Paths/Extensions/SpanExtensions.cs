using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.Paths.HighPerformance.CommunityToolkit;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Extensions;

/// <summary>
/// Extension methods tied to spans.
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    /// Casts a span to another type without bounds checks.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Taken from runtime.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<TTo> CastFast<TFrom, TTo>(this Span<TFrom> data) where TFrom : struct where TTo : struct
    {
        // Taken from the runtime.
        // Use unsigned integers - unsigned division by constant (especially by power of 2)
        // and checked casts are faster and smaller.
        uint fromSize = (uint)Unsafe.SizeOf<TFrom>();
        uint toSize = (uint)Unsafe.SizeOf<TTo>();
        uint fromLength = (uint)data.Length;
        int toLength;
        if (fromSize == toSize)
        {
            // Special case for same size types - `(ulong)fromLength * (ulong)fromSize / (ulong)toSize`
            // should be optimized to just `length` but the JIT doesn't do that today.
            toLength = (int)fromLength;
        }
        else if (fromSize == 1)
        {
            // Special case for byte sized TFrom - `(ulong)fromLength * (ulong)fromSize / (ulong)toSize`
            // becomes `(ulong)fromLength / (ulong)toSize` but the JIT can't narrow it down to `int`
            // and can't eliminate the checked cast. This also avoids a 32 bit specific issue,
            // the JIT can't eliminate long multiply by 1.
            toLength = (int)(fromLength / toSize);
        }
        else
        {
            // Ensure that casts are done in such a way that the JIT is able to "see"
            // the uint->ulong casts and the multiply together so that on 32 bit targets
            // 32x32to64 multiplication is used.
            ulong toLengthUInt64 = fromLength * (ulong)fromSize / toSize;
            toLength = (int)toLengthUInt64;
        }
        
        return MemoryMarshal.CreateSpan(
            ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(data)),
            toLength);
    }
    
    /// <summary>
    /// Slices a span without any bounds checks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> SliceFast<T>(this ReadOnlySpan<T> data, int start)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), data.Length - start);
    }
    
    /// <summary>
    /// Slices a span without any bounds checks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> SliceFast<T>(this ReadOnlySpan<T> data, int start, int length)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), length);
    }
    
    /// <summary>
    /// Slices a span without any bounds checks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> SliceFast<T>(this Span<T> data, int start)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), data.Length - start);
    }
    
    /// <summary>
    /// Slices a span without any bounds checks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> SliceFast<T>(this Span<T> data, int start, int length)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), length);
    }
    
    /// <summary>
    /// Replaces the occurrences of one character with another in a span.
    /// </summary>
    /// <param name="data">The data to replace the value in.</param>
    /// <param name="oldValue">The original value to be replaced.</param>
    /// <param name="newValue">The new replaced value.</param>
    /// <param name="buffer">
    ///    The buffer to place the result in.
    ///    This can be the original <paramref name="data"/> buffer if required.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<char> Replace(this Span<char> data, char oldValue, char newValue, Span<char> buffer)
    {
        // char is not supported by Vector; but ushort is.
        return Replace(data.CastFast<char, ushort>(), oldValue, newValue, buffer.CastFast<char, ushort>())
               .CastFast<ushort, char>();
    }

    /// <summary>
    /// Replaces the occurrences of one value with another in a span.
    /// </summary>
    /// <param name="data">The data to replace the value in.</param>
    /// <param name="oldValue">The original value to be replaced.</param>
    /// <param name="newValue">The new replaced value.</param>
    /// <param name="buffer">
    ///    The buffer to place the result in.
    ///    This can be the original <paramref name="data"/> buffer if required.
    /// </param>
    /// <paramref name="TType">MUST BE POWER OF TWO IN SIZE. Type of value to replace.</paramref>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> Replace<T>(this Span<T> data, T oldValue, T newValue, Span<T> buffer) where T : unmanaged, IEquatable<T>
    {
        // In the case they are the same, do nothing.
        if (oldValue.Equals(newValue))
            return data;
        
        // Vectorised Span item replace by Sewer56 
        if (data.Length > buffer.Length)
        {
            ThrowHelpers.InsufficientMemoryException($"Length of '{nameof(buffer)}' passed into {nameof(Replace)} is insufficient.");
            return data;
        }
        
        // Slice our output buffer.
        buffer = buffer.SliceFast(0, data.Length);
        nuint remainingLength = (nuint)data.Length;
        
        // Copy the remaining characters, doing the replacement as we go.
        // Note: We can index 0 directly since we know length is >0 given length check from earlier.
        ref T pSrc = ref data[0];
        ref T pDst = ref buffer[0];
        nuint x = 0;

        if (Vector.IsHardwareAccelerated && data.Length >= Vector<T>.Count)
        {
            Vector<T> oldValues = new(oldValue);
            Vector<T> newValues = new(newValue);

            Vector<T> original;
            Vector<T> equals;
            Vector<T> results;

            if (remainingLength > (nuint)Vector<T>.Count)
            {
                nuint lengthToExamine = remainingLength - (nuint)Vector<T>.Count;

                do
                {
                    original = VectorExtensions.LoadUnsafe(ref pSrc, x);
                    equals   = Vector.Equals(original, oldValues); // Generate Mask
                    results  = Vector.ConditionalSelect(equals, newValues, original); // Swap in Values
                    results.StoreUnsafe(ref pDst, x);

                    x += (nuint)Vector<T>.Count;
                }
                while (x < lengthToExamine);
            }

            // There are between 0 to Vector<T>.Count elements remaining now.  
            
            // Since our operation can be applied multiple times without changing the result
            // [applying the replacement twice is non destructive]. We can avoid non-vectorised code
            // here and simply do the vectorised logic in an unaligned fashion, doing just the chunk
            // at the end of the original buffer.
            x = (uint)(data.Length - Vector<T>.Count);
            original = VectorExtensions.LoadUnsafe(ref data[0], x);
            equals   = Vector.Equals(original, oldValues);
            results  = Vector.ConditionalSelect(equals, newValues, original);
            results.StoreUnsafe(ref buffer[0], x);
        }
        else
        {
            // Non-vector fallback, slow.
            for (; x < remainingLength; ++x)
            {
                T currentChar = Unsafe.Add(ref pSrc, x);
                Unsafe.Add(ref pDst, x) = currentChar.Equals(oldValue) ? newValue : currentChar;
            }
        }

        return buffer;
    }
    
    /// <summary>
    /// Counts the number of occurrences of a given value into a target <see cref="Span{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items in the input <see cref="Span{T}"/> instance.</typeparam>
    /// <param name="span">The input <see cref="Span{T}"/> instance to read.</param>
    /// <param name="value">The <typeparamref name="T"/> value to look for.</param>
    /// <returns>The number of occurrences of <paramref name="value"/> in <paramref name="span"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
    {
        ref T r0 = ref MemoryMarshal.GetReference(span);
        nint length = (nint)(uint)span.Length;

        return (int)SpanHelper.Count(ref r0, length, value);
    }
}