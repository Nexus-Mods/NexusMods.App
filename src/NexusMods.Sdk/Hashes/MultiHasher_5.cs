using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public record struct MultiHash<THash1, THash2, THash3, THash4, THash5>(THash1 Hash1, THash2 Hash2, THash3 Hash3, THash4 Hash4, THash5 Hash5)
    where THash1 : unmanaged, IEquatable<THash1>
    where THash2 : unmanaged, IEquatable<THash2>
    where THash3 : unmanaged, IEquatable<THash3>
    where THash4 : unmanaged, IEquatable<THash4>
    where THash5 : unmanaged, IEquatable<THash5>;

[PublicAPI]
public class MultiHashState<TState1, TState2, TState3, TState4, TState5>
{
    public required TState1 State1 { get; set; }
    public required TState2 State2 { get; set; }
    public required TState3 State3 { get; set; }
    public required TState4 State4 { get; set; }
    public required TState5 State5 { get; set; }
}

[PublicAPI]
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

    public static ValueTask<MultiHash<THash1, THash2, THash3, THash4, THash5>> HashAsync(Stream stream, int bufferSize = IStreamingHasher<MultiHash<THash1, THash2, THash3, THash4, THash5>, MultiHashState<TState1, TState2, TState3, TState4, TState5>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>>.DefaultBufferSize, CancellationToken cancellationToken = default)
    {
        return StreamingHasher<MultiHash<THash1, THash2, THash3, THash4, THash5>, MultiHashState<TState1, TState2, TState3, TState4, TState5>, MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>>.HashAsync(stream, bufferSize, cancellationToken);
    }
}

[PublicAPI]
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
    public static readonly MultiHasherBuilder<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5> Instance;

    public MultiHash<THash1, THash2, THash3, THash4, THash5> Hash(ReadOnlySpan<byte> input)
    {
        return MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>.Hash(input);
    }

    public ValueTask<MultiHash<THash1, THash2, THash3, THash4, THash5>> HashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return MultiHasher<THash1, TState1, THasher1, THash2, TState2, THasher2, THash3, TState3, THasher3, THash4, TState4, THasher4, THash5, TState5, THasher5>.HashAsync(stream, cancellationToken: cancellationToken);
    }
}
