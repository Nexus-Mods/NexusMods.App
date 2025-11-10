namespace NexusMods.Sdk.Hashes;

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
