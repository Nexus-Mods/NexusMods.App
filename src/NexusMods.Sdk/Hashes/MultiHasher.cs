using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Sdk.Hashes;

public record struct MultiHash<THash1, THash2, THash3, THash4, THash5>(THash1 Hash1, THash2 Hash2, THash3 Hash3, THash4 Hash4, THash5 Hash5)
    where THash1 : unmanaged, IEquatable<THash1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THash4 : unmanaged, IEquatable<THash4>
    where THash5 : unmanaged, IEquatable<THash5>;

public class MultiHashState<TState1, TState2, TState3, TState4, TState5>
{
    public required TState1 State1 { get; set; }
    public required TState2 State2 { get; set; }
    public required TState3 State3 { get; set; }
    public required TState4 State4 { get; set; }
    public required TState5 State5 { get; set; }
}

public class MultiHasher<
    THash1, TState1, THasher1,
    THash2, TState2, THasher2,
    THash3, TState3, THasher3,
    THash4, TState4, THasher4,
    THash5, TState5, THasher5
> : IStreamingHasher<
    MultiHash<THash1, THash2, THash3, THash4, THash5>,
    MultiHashState<TState1, TState2, TState3, TState4, TState5>,
    MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>
>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
    where THash4 : unmanaged, IEquatable<THash4>
    where THasher4 : IStreamingHasher<THash4, TState4, THasher4>
    where THash5 : unmanaged, IEquatable<THash5>
    where THasher5 : IStreamingHasher<THash5, TState5, THasher5>
{
    public static MultiHash<THash1, THash2, THash3, THash4, THash5> Hash(ReadOnlySpan<byte> input)
    {
        var hash1 = THasher1.Hash(input);
        var hash2 = THasher2.Hash(input);
        var hash3 = THasher3.Hash(input);
        var hash4 = THasher4.Hash(input);
        var hash5 = THasher5.Hash(input);
        return new MultiHash<THash1, THash2, THash3, THash4, THash5>(hash1, hash2, hash3, hash4, hash5);
    }

    public static MultiHashState<TState1, TState2, TState3, TState4, TState5> Initialize()
    {
        return new MultiHashState<TState1, TState2, TState3, TState4, TState5>
        {
            State1 = THasher1.Initialize(),
            State2 = THasher2.Initialize(),
            State3 = THasher3.Initialize(),
            State4 = THasher4.Initialize(),
            State5 = THasher5.Initialize(),
        };
    }

    public static MultiHashState<TState1, TState2, TState3, TState4, TState5> Update(MultiHashState<TState1, TState2, TState3, TState4, TState5> state, ReadOnlySpan<byte> input)
    {
        state.State1 = THasher1.Update(state.State1, input);
        state.State2 = THasher2.Update(state.State2, input);
        state.State3 = THasher3.Update(state.State3, input);
        state.State4 = THasher4.Update(state.State4, input);
        state.State5 = THasher5.Update(state.State5, input);
        return state;
    }

    public static MultiHashState<TState1, TState2, TState3, TState4, TState5> Update(MultiHashState<TState1, TState2, TState3, TState4, TState5> state, byte[] input)
    {
        state.State1 = THasher1.Update(state.State1, input);
        state.State2 = THasher2.Update(state.State2, input);
        state.State3 = THasher3.Update(state.State3, input);
        state.State4 = THasher4.Update(state.State4, input);
        state.State5 = THasher5.Update(state.State5, input);
        return state;
    }

    public static MultiHash<THash1, THash2, THash3, THash4, THash5> Finish(MultiHashState<TState1, TState2, TState3, TState4, TState5> state)
    {
        var hash1 = THasher1.Finish(state.State1);
        var hash2 = THasher2.Finish(state.State2);
        var hash3 = THasher3.Finish(state.State3);
        var hash4 = THasher4.Finish(state.State4);
        var hash5 = THasher5.Finish(state.State5);
        return new MultiHash<THash1, THash2, THash3, THash4, THash5>(hash1, hash2, hash3, hash4, hash5);
    }
}

public readonly struct MultiHasherBuilder
{
    private static readonly MultiHasherBuilder Instance = new();
    public static MultiHasherBuilder Start() => Instance;

    public MultiHasherBuilder<THash1, TState1, THasher1> AddHasher<THash1, TState1, THasher1>()
        where THash1 : unmanaged, IEquatable<THash1>
        where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    {
        return MultiHasherBuilder<THash1, TState1, THasher1>.Instance;
    }
}

public readonly struct MultiHasherBuilder<THash1, TState1, THasher1>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
{
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1> Instance = new();

    public MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2> AddHasher<THash2, TState2, THasher2>()
        where THash2 : unmanaged, IEquatable<THash2>
        where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
    {
        return MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2>.Instance;
    }
}

public readonly struct MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
{
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2> Instance = new();

    public MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3> AddHasher<THash3, TState3, THasher3>()
        where THash3 : unmanaged, IEquatable<THash3>
        where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
    {
        return MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>.Instance;
    }
}

public readonly struct MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
{
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3> Instance = new();

    public MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4> AddHasher<THash4, TState4, THasher4>()
        where THash4 : unmanaged, IEquatable<THash4>
        where THasher4 : IStreamingHasher<THash4, TState4, THasher4>
    {
        return MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4>.Instance;
    }
}

public readonly struct MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
    where THash4 : unmanaged, IEquatable<THash4>
    where THasher4 : IStreamingHasher<THash4, TState4, THasher4>
{
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4> Instance = new();

    public MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5> AddHasher<THash5, TState5, THasher5>()
        where THash5 : unmanaged, IEquatable<THash5>
        where THasher5 : IStreamingHasher<THash5, TState5, THasher5>
    {
        return MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>.Instance;
    }
}

public readonly struct MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
    where THash4 : unmanaged, IEquatable<THash4>
    where THasher4 : IStreamingHasher<THash4, TState4, THasher4>
    where THash5 : unmanaged, IEquatable<THash5>
    where THasher5 : IStreamingHasher<THash5, TState5, THasher5>
{
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5> Instance = new();

    public MultiHash<THash1, THash2, THash3, THash4, THash5> Hash(ReadOnlySpan<byte> input)
    {
        return Hasher<MultiHash<THash1, THash2, THash3, THash4, THash5>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>>.Hash(input);
    }

    public ValueTask<MultiHash<THash1, THash2, THash3, THash4, THash5>> HashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return StreamingHasher<MultiHash<THash1, THash2, THash3, THash4, THash5>, MultiHashState<TState1, TState2, TState3, TState4, TState5>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>>.HashAsync(stream, cancellationToken: cancellationToken);
    }
}

[PublicAPI]
public static class MultiHasher
{
    private const int BufferSize = 64 * 1024;
    private const int MaxFullFileHash = BufferSize * 2;

    private static readonly MultiHasherBuilder<Crc32Value, Crc32, Crc32Hasher, Md5Value, MD5, Md5Hasher, Sha1Value, SHA1, Sha1Hasher, Hash, XxHash3, Xx3Hasher, Hash, XxHash64, Xx64Hasher> Hasher;

    static MultiHasher()
    {
        Hasher = MultiHasherBuilder.Start()
            .AddHasher<Crc32Value, Crc32, Crc32Hasher>()
            .AddHasher<Md5Value, MD5, Md5Hasher>()
            .AddHasher<Sha1Value, SHA1, Sha1Hasher>()
            .AddHasher<Hash, XxHash3, Xx3Hasher>()
            .AddHasher<Hash, XxHash64, Xx64Hasher>();
    }

    public static async Task<MultiHash> HashStream(Stream stream, CancellationToken cancellationToken = default)
    {
        stream.Position = 0;
        var multiHash = await Hasher.HashAsync(stream, cancellationToken: cancellationToken);
        var minimalHash = await MinimalHash<Hash, XxHash3, Xx3Hasher>(stream, cancellationToken: cancellationToken);

        var result = new MultiHash
        {
            Crc32 = multiHash.Hash1,
            Md5 = multiHash.Hash2,
            Sha1 = multiHash.Hash3,
            XxHash3 = multiHash.Hash4,
            XxHash64 = multiHash.Hash5,

            MinimalHash = minimalHash,
            Size = Size.FromLong(stream.Length),
        };

        return result;
    }

    public static async ValueTask<THash> MinimalHash<THash, TState, THasher>(Stream stream, CancellationToken cancellationToken = default)
        where THash : unmanaged, IEquatable<THash>
        where THasher : IStreamingHasher<THash, TState, THasher>
    {
        stream.Position = 0;
        if (stream.Length <= MaxFullFileHash) return await THasher.HashAsync(stream, cancellationToken: cancellationToken);

        var buffer = GC.AllocateUninitializedArray<byte>(BufferSize);
        var state = THasher.Initialize();

        // Read the block at the start of the file
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        state = THasher.Update(state, buffer);

        // Read the block at the end of the file
        stream.Position = stream.Length - BufferSize;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        state = THasher.Update(state, buffer);

        // Read the block in the middle, if the file is too small, offset the middle enough to not read past the end
        // of the file
        var middleOffset = Math.Min(stream.Length / 2, stream.Length - BufferSize);
        stream.Position = middleOffset;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        state = THasher.Update(state, buffer);

        // Add the length of the file to the hash (as an ulong)
        Span<byte> lengthBuffer = stackalloc byte[sizeof(ulong)];
        MemoryMarshal.Write(lengthBuffer, (ulong)stream.Length);
        state = THasher.Update(state, lengthBuffer);

        return THasher.Finish(state);
    }
}
