using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Represents a streaming hash algorithm.
/// </summary>
[PublicAPI]
public interface IStreamingHasher<THash, TState, TSelf> : IHasher<THash, TSelf>
    where THash : unmanaged, IEquatable<THash>
    where TSelf : IStreamingHasher<THash, TState, TSelf>
{
    internal const int DefaultBufferSize = 1024 * 8;

    /// <summary>
    /// Hashes the stream contents until the end.
    /// </summary>
    static virtual async ValueTask<THash> HashAsync(Stream stream, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
    {
        var buffer = GC.AllocateUninitializedArray<byte>(bufferSize);

        var state = TSelf.Initialize();
        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;

            state = TSelf.Update(state, buffer);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return TSelf.Finish(state);
    }

    /// <summary>
    /// Initializes the streaming state.
    /// </summary>
    static abstract TState Initialize();

    /// <summary>
    /// Updates the streaming state.
    /// </summary>
    static abstract TState Update(TState state, ReadOnlySpan<byte> input);

    /// <summary>
    /// Updates the streaming state.
    /// </summary>
    static virtual TState Update(TState state, byte[] input) => TSelf.Update(state, input.AsSpan());

    /// <summary>
    /// Finalizes the streaming state.
    /// </summary>
    static abstract THash Finish(TState state);
}

[PublicAPI]
public static class StreamingHasher<THash, TState, THasher>
    where THash : unmanaged, IEquatable<THash>
    where THasher : IStreamingHasher<THash, TState, THasher>
{
    /// <inheritdoc cref="IStreamingHasher{THash,TState,TSelf}.HashAsync"/>
    public static ValueTask<THash> HashAsync(
        Stream stream,
        int bufferSize = IStreamingHasher<THash, TState, THasher>.DefaultBufferSize,
        CancellationToken cancellationToken = default) => THasher.HashAsync(stream, bufferSize, cancellationToken);
}
