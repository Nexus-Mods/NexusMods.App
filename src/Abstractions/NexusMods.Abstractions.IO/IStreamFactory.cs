using NexusMods.Paths;

namespace NexusMods.Abstractions.IO;

/// <summary>
///     A way of creating a stream from another object. Could be an entry in an archive, lazy extracted, could be a
///     file on disk, or some network resource.
/// </summary>
public interface IStreamFactory
{
    /// <summary>
    ///     The Path of the stream.
    /// </summary>
    IPath Name { get; }

    /// <summary>
    ///     The size of the stream
    /// </summary>
    Size Size { get; }

    /// <summary>
    ///     Returns a read-only stream for the given factory
    /// </summary>
    /// <returns></returns>
    ValueTask<Stream> GetStreamAsync();
}
