using System.Runtime.CompilerServices;

namespace NexusMods.Hashing.xxHash64;

/// <summary>
///     Based on the code found at (https://github.com/brandondahler/Data.HashFunction/)
///     The MIT License (MIT)
///     Copyright (c) 2014 Data.HashFunction Developers
///     Permission is hereby granted, free of charge, to any person obtaining a copy
///     of this software and associated documentation files (the "Software"), to deal
///     in the Software without restriction, including without limitation the rights
///     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
///     copies of the Software, and to permit persons to whom the Software is
///     furnished to do so, subject to the following conditions:
///     The above copyright notice and this permission notice shall be included in all
///     copies or substantial portions of the Software.
///     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
///     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
///     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
///     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
///     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
///     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
///     SOFTWARE.
/// </summary>
public struct xxHashAlgorithm
{
    private static readonly IReadOnlyList<ulong> Primes64 =
        new[]
        {
            11400714785074694791UL,
            14029467366897019727UL,
            1609587929392839161UL,
            9650029242287828579UL,
            2870177450012600261UL
        };


    private readonly ulong _seed;

    private ulong _a;
    private ulong _b;
    private ulong _c;
    private ulong _d;

    private ulong _bytesProcessed;
    private readonly bool _finished;

    public xxHashAlgorithm(ulong seed)
    {
        _seed = seed;
        _a = _seed + Primes64[0] + Primes64[1];
        _b = _seed + Primes64[1];
        _c = _seed;
        _d = _seed - Primes64[0];
        _bytesProcessed = 0;
        _finished = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong HashBytes(ReadOnlySpan<byte> data)
    {
        var initialSize = (data.Length >> 5) << 5;
        if (initialSize > 0) TransformByteGroupsInternal(data[..initialSize]);

        return FinalizeHashValueInternal(data[initialSize..]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TransformByteGroupsInternal(ReadOnlySpan<byte> data)
    {
        if (_finished || data.Length % 32 > 0)
            throw new Exception("Hash is finished, or input is not a multiple of 32");
        var tempA = _a;
        var tempB = _b;
        var tempC = _c;
        var tempD = _d;

        var tempPrime0 = Primes64[0];
        var tempPrime1 = Primes64[1];

        for (var currentIndex = 0; currentIndex < data.Length; currentIndex += 32)
        {
            tempA += BitConverter.ToUInt64(data[currentIndex..]) * tempPrime1;
            tempA = RotateLeft(tempA, 31);
            tempA *= tempPrime0;

            tempB += BitConverter.ToUInt64(data[(currentIndex + 8)..]) * tempPrime1;
            tempB = RotateLeft(tempB, 31);
            tempB *= tempPrime0;

            tempC += BitConverter.ToUInt64(data[(currentIndex + 16)..]) * tempPrime1;
            tempC = RotateLeft(tempC, 31);
            tempC *= tempPrime0;

            tempD += BitConverter.ToUInt64(data[(currentIndex + 24)..]) * tempPrime1;
            tempD = RotateLeft(tempD, 31);
            tempD *= tempPrime0;
        }

        _a = tempA;
        _b = tempB;
        _c = tempC;
        _d = tempD;

        _bytesProcessed += (ulong) data.Length;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong FinalizeHashValueInternal(ReadOnlySpan<byte> data)
    {
        ulong hashValue;
        {
            if (_bytesProcessed > 0)
            {
                var tempA = _a;
                var tempB = _b;
                var tempC = _c;
                var tempD = _d;


                hashValue = RotateLeft(_a, 1) + RotateLeft(_b, 7) + RotateLeft(_c, 12) + RotateLeft(_d, 18);

                // A
                tempA *= Primes64[1];
                tempA = RotateLeft(tempA, 31);
                tempA *= Primes64[0];

                hashValue ^= tempA;
                hashValue = hashValue * Primes64[0] + Primes64[3];

                // B
                tempB *= Primes64[1];
                tempB = RotateLeft(tempB, 31);
                tempB *= Primes64[0];

                hashValue ^= tempB;
                hashValue = hashValue * Primes64[0] + Primes64[3];

                // C
                tempC *= Primes64[1];
                tempC = RotateLeft(tempC, 31);
                tempC *= Primes64[0];

                hashValue ^= tempC;
                hashValue = hashValue * Primes64[0] + Primes64[3];

                // D
                tempD *= Primes64[1];
                tempD = RotateLeft(tempD, 31);
                tempD *= Primes64[0];

                hashValue ^= tempD;
                hashValue = hashValue * Primes64[0] + Primes64[3];
            }
            else
            {
                hashValue = _seed + Primes64[4];
            }
        }

        var remainderLength = data.Length;

        hashValue += _bytesProcessed + (ulong) remainderLength;

        if (remainderLength > 0)
        {
            // In 8-byte chunks, process all full chunks
            for (var x = 0; x < data.Length / 8; ++x)
            {
                hashValue ^= RotateLeft(BitConverter.ToUInt64(data[(x * 8)..]) * Primes64[1], 31) * Primes64[0];
                hashValue = RotateLeft(hashValue, 27) * Primes64[0] + Primes64[3];
            }

            // Process a 4-byte chunk if it exists
            if (remainderLength % 8 >= 4)
            {
                var startOffset = remainderLength - remainderLength % 8;

                hashValue ^= BitConverter.ToUInt32(data[startOffset..]) * Primes64[0];
                hashValue = RotateLeft(hashValue, 23) * Primes64[1] + Primes64[2];
            }

            // Process last 4 bytes in 1-byte chunks (only runs if data.Length % 4 != 0)
            {
                var startOffset = remainderLength - remainderLength % 4;
                var endOffset = remainderLength;

                for (var currentOffset = startOffset; currentOffset < endOffset; currentOffset += 1)
                {
                    hashValue ^= data[currentOffset] * Primes64[4];
                    hashValue = RotateLeft(hashValue, 11) * Primes64[0];
                }
            }
        }

        hashValue ^= hashValue >> 33;
        hashValue *= Primes64[1];
        hashValue ^= hashValue >> 29;
        hashValue *= Primes64[2];
        hashValue ^= hashValue >> 32;

        return hashValue;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RotateLeft(ulong operand, int shiftCount)
    {
        shiftCount &= 0x3f;

        return
            (operand << shiftCount) |
            (operand >> (64 - shiftCount));
    }
}