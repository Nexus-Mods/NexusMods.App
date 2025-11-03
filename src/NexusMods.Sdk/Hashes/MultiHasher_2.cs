using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public record struct MultiHash<THash1, THash2>(THash1 Hash1, THash2 Hash2)
    where THash1 : unmanaged, IEquatable<THash1>
    where THash2 : unmanaged, IEquatable<THash2>;

[PublicAPI]
public class MultiHashState<TState1, TState2>
{
    public required TState1 State1 { get; set; }
    public required TState2 State2 { get; set; }
}

[PublicAPI]
public class MultiHasher<
    THash1, TState1, THasher1,
    THash2, TState2, THasher2
> : IStreamingHasher<
    MultiHash<THash1, THash2>,
    MultiHashState<TState1, TState2>,
    MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2>
>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
{
    public static MultiHash<THash1, THash2> Hash(ReadOnlySpan<byte> input)
    {
        var hash1 = THasher1.Hash(input);
        var hash2 = THasher2.Hash(input);
        return new MultiHash<THash1, THash2>(hash1, hash2);
    }

    public static MultiHashState<TState1, TState2> Initialize()
    {
        return new MultiHashState<TState1, TState2>
        {
            State1 = THasher1.Initialize(),
            State2 = THasher2.Initialize(),
        };
    }

    public static MultiHashState<TState1, TState2> Update(MultiHashState<TState1, TState2> state, ReadOnlySpan<byte> input)
    {
        state.State1 = THasher1.Update(state.State1, input);
        state.State2 = THasher2.Update(state.State2, input);
        return state;
    }

    public static MultiHashState<TState1, TState2> Update(MultiHashState<TState1, TState2> state, byte[] input, int offset, int count)
    {
        state.State1 = THasher1.Update(state.State1, input, offset, count);
        state.State2 = THasher2.Update(state.State2, input, offset, count);
        return state;
    }

    public static MultiHash<THash1, THash2> Finish(MultiHashState<TState1, TState2> state)
    {
        var hash1 = THasher1.Finish(state.State1);
        var hash2 = THasher2.Finish(state.State2);
        return new MultiHash<THash1, THash2>(hash1, hash2);
    }

    public static ValueTask<MultiHash<THash1, THash2>> HashAsync(Stream stream, int bufferSize = IStreamingHasher<MultiHash<THash1, THash2>, MultiHashState<TState1, TState2>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2>>.DefaultBufferSize, CancellationToken cancellationToken = default)
    {
        return StreamingHasher<MultiHash<THash1, THash2>, MultiHashState<TState1, TState2>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2>>.HashAsync(stream, bufferSize, cancellationToken);
    }
}

[PublicAPI]
public readonly struct MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
{
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2> Instance;

    public MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3> AddHasher<THash3, TState3, THasher3>()
        where THash3 : unmanaged, IEquatable<THash3>
        where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
    {
        return MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>.Instance;
    }

    public MultiHash<THash1, THash2> Hash(ReadOnlySpan<byte> input)
    {
        return MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2>.Hash(input);
    }

    public ValueTask<MultiHash<THash1, THash2>> HashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2>.HashAsync(stream, cancellationToken: cancellationToken);
    }
}
