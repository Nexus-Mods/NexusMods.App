# Use xxHash64 for File Hashing

## Context and Problem Statement

Several aspects of the app require a fast and efficient file hashing routine. The undo functionality is based on content hashing many of the files
involved in the modding process.

## Decision Drivers

* Speed of the hashing algorithm
    * Should be able to keep up with a modern NVME drive and a 4 core system
* Ease of implementation
    * We may be asking Nexus Mods content servers to also implement this hash, so a involved complex algorithm may not be applicable
* Low collision rate
    * Having two files collide on hashes would be catastrophic. Something like CRC will not work here
* Need not be cryptographic
    * None of these systems deal with security concerns, so we need not consider only cryptographic hashes
* Streaming support
    * Several files are quite large (10GB+) the algorithm should not require all the contents to be in memory at one time

## Considered Options

* MD5
* SHA256
* xxHash64
* xxHash3 - 64bit
* xxHash3 - 128bit

## Decision Outcome

Chosen option: xxHash64 provides the best balance of simplicity, performance, and uniqueness.

### Consequences

* Good, because {positive consequence, e.g., improvement of one or more desired qualities, …}
* Bad, because {negative consequence, e.g., compromising one or more desired qualities, …}
* … <!-- numbers of consequences can vary -->

<!-- This is an optional element. Feel free to remove. -->
## Validation

Benchmarks were performed on several hashing algorithms and several implementations of these algorithms. These benchmarks
are the time taken to hash 1GB of in-memory data. These benchmarks are for a single core, extrapolation can be done to estimate performance if multiple cores are used.

| Method                 |        Mean |    Error |   StdDev | Allocated | GB/sec |
|------------------------|------------:|---------:|---------:|----------:|-------:|
| WJ_xxHash64 (Async)    |   106.54 ms | 1.286 ms | 1.140 ms |     323 B | 9.3    |
| WJ_xxHash64_Sync       |    83.08 ms | 0.666 ms | 0.623 ms |      96 B | 12.03  |
| SystemIO_xxHash64      |    86.74 ms | 0.735 ms | 0.902 ms |     272 B | 11.61  |
| YYProject_xxHash3      |    82.71 ms | 0.327 ms | 0.290 ms |      69 B | 12.09  |
| YYProject_xxHash3_SSE2 |    30.36 ms | 0.487 ms | 0.455 ms |      15 B | 32.93  |
| YYProject_xxHash3_AVX2 |    21.23 ms | 0.421 ms | 0.604 ms |      15 B | 47.10  |
| SystemIO_xxHash32      |   554.36 ms | 0.844 ms | 0.748 ms |     512 B | 1.08   |
| MD5_Hash               | 1,313.31 ms | 1.444 ms | 1.280 ms |     688 B | 0.76   |
| SHA1_Hash              | 1,029.47 ms | 3.745 ms | 3.503 ms |     704 B | 0.97   |
| SHA256_Hash            |   399.40 ms | 2.913 ms | 2.725 ms |     720 B | 2.5    |
| System_CRC64           | 1,576.60 ms | 6.169 ms | 5.771 ms |     512 B | 0.63   |
| System_CRC32           | 1,774.06 ms | 2.995 ms | 2.655 ms |     512 B | 0.56   |

Test System:

* CPU: Ryzen 9 7950x @ 5.35Ghz
* RAM: DDR5 6000Mhz CL30

The relatively high performance of the SHA256 hash is assumed to be related to support for AES instructions in .NET and the related
algorithms.

While 600MB/sec of hashing may sound rather fast, this is the *starting* speed for SSD drives. Modern SATA SSDs can easily saturate
a 600MB/sec SATA connection, and entry level NVME drives start at 1.5GB/sec and go up to 3GB/sec on high end models. As of the time
of this writing, NVME 4.0 drives are fairly commonplace on high end systems (~6GB/sec read speeds), and NVME 5.0 drives are expected
to start appearing on the market in the next 6 months (~12GB/sec). This mostly rules out any of the cryptographic algorithms
due to their relatively low performance on low-core-count systems. xxHash3 is extremely fast, but the implementation has been expanded
quite a bit from the previous incarnation (xxHash64) in order to offer higher performance on smaller hash sizes (less than 128 bytes).
In addition xxHash3 only performs best when backed up by AVX2 and SSE2 instructions which further complicate the implementation.

The xxHash3 implementation is not extremely complex but is complex enough that a junior level programmer may have problems understanding
and implementing the algorithm. Contrast this with xxHash64 which comes in at less than 200 lines of C# code. This extreme simplicity
of xxHash64 combined with its fantastic performance, makes it the best option for hashing in this application.

## xxHash64 Algorithm in C# (for reference)
```csharp
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

    public ulong HashBytes(ReadOnlySpan<byte> data)
    {
        var initialSize = (data.Length >> 5) << 5;
        if (initialSize > 0) TransformByteGroupsInternal(data[..initialSize]);

        return FinalizeHashValueInternal(data[initialSize..]);
    }

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

        _finished = true;
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
```
