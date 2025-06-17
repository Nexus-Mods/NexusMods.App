using NexusMods.Paths;

namespace NexusMods.Sdk.IO;

/// <summary>
///     A way of creating a stream from another object. Could be an entry in an archive, lazy extracted, could be a
///     file on disk, or some network resource.
/// </summary>
public interface IStreamFactory
{
    /// <summary>
    /// Gets the name of the file that the stream is based on or <see cref="RelativePath.Empty"/> if there
    /// isn't a file.
    /// </summary>
    RelativePath FileName { get; }

    /// <summary>
    ///Returns a read-only stream for the given factory
    /// </summary>
    ValueTask<Stream> GetStreamAsync();
}
