using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public record struct MultiHash<THash1, THash2, THash3>(THash1 Hash1, THash2 Hash2, THash3 Hash3)
    where THash1 : unmanaged, IEquatable<THash1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THash3 : unmanaged, IEquatable<THash3>;

[PublicAPI]
public class MultiHashState<TState1, TState2, TState3>
{
    public required TState1 State1 { get; set; }
    public required TState2 State2 { get; set; }
    public required TState3 State3 { get; set; }
}

[PublicAPI]
public class MultiHasher<
    THash1, TState1, THasher1,
    THash2, TState2, THasher2,
    THash3, TState3, THasher3
> : IStreamingHasher<
    MultiHash<THash1, THash2, THash3>,
    MultiHashState<TState1, TState2, TState3>,
    MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>
>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
{
    public static MultiHash<THash1, THash2, THash3> Hash(ReadOnlySpan<byte> input)
    {
        var hash1 = THasher1.Hash(input);
        var hash2 = THasher2.Hash(input);
        var hash3 = THasher3.Hash(input);
        return new MultiHash<THash1, THash2, THash3>(hash1, hash2, hash3);
    }

    public static MultiHashState<TState1, TState2, TState3> Initialize()
    {
        return new MultiHashState<TState1, TState2, TState3>
        {
            State1 = THasher1.Initialize(),
            State2 = THasher2.Initialize(),
            State3 = THasher3.Initialize(),
        };
    }

    public static MultiHashState<TState1, TState2, TState3> Update(MultiHashState<TState1, TState2, TState3> state, ReadOnlySpan<byte> input)
    {
        state.State1 = THasher1.Update(state.State1, input);
        state.State2 = THasher2.Update(state.State2, input);
        state.State3 = THasher3.Update(state.State3, input);
        return state;
    }

    public static MultiHashState<TState1, TState2, TState3> Update(MultiHashState<TState1, TState2, TState3> state, byte[] input, int offset, int count)
    {
        state.State1 = THasher1.Update(state.State1, input, offset, count);
        state.State2 = THasher2.Update(state.State2, input, offset, count);
        state.State3 = THasher3.Update(state.State3, input, offset, count);
        return state;
    }

    public static MultiHash<THash1, THash2, THash3> Finish(MultiHashState<TState1, TState2, TState3> state)
    {
        var hash1 = THasher1.Finish(state.State1);
        var hash2 = THasher2.Finish(state.State2);
        var hash3 = THasher3.Finish(state.State3);
        return new MultiHash<THash1, THash2, THash3>(hash1, hash2, hash3);
    }

    public static ValueTask<MultiHash<THash1, THash2, THash3>> HashAsync(Stream stream, int bufferSize = IStreamingHasher<MultiHash<THash1, THash2, THash3>, MultiHashState<TState1, TState2, TState3>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>>.DefaultBufferSize, CancellationToken cancellationToken = default)
    {
        return StreamingHasher<MultiHash<THash1, THash2, THash3>, MultiHashState<TState1, TState2, TState3>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>>.HashAsync(stream, bufferSize, cancellationToken);
    }
}

[PublicAPI]
public readonly struct MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>
    where THash1 : unmanaged, IEquatable<THash1>
    where THasher1 : IStreamingHasher<THash1, TState1, THasher1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THasher2 : IStreamingHasher<THash2, TState2, THasher2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THasher3 : IStreamingHasher<THash3, TState3, THasher3>
{
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3> Instance;

    public MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4> AddHasher<THash4, TState4, THasher4>()
        where THash4 : unmanaged, IEquatable<THash4>
        where THasher4 : IStreamingHasher<THash4, TState4, THasher4>
    {
        return MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4>.Instance;
    }

    public MultiHash<THash1, THash2, THash3> Hash(ReadOnlySpan<byte> input)
    {
        return MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>.Hash(input);
    }

    public ValueTask<MultiHash<THash1, THash2, THash3>> HashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3>.HashAsync(stream, cancellationToken: cancellationToken);
    }
}
