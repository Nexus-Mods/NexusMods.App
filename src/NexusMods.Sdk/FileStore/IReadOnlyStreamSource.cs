using NexusMods.Hashing.xxHash3;

namespace NexusMods.Sdk.FileStore;

/// <summary>
/// Provides a read-only stream source. Streams can be opened by hash.
/// </summary>
public interface IReadOnlyStreamSource
{
    /// <summary>
    /// Lookup a stream by hash. The result will be seekable
    /// </summary>
    public ValueTask<Stream?> OpenAsync(Hash hash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Return true if the stream exists in the source
    /// </summary>
    public bool Exists(Hash hash);
    
    /// <summary>
    /// Get the priority of this source.
    /// </summary>
    public SourcePriority Priority { get; }
}
