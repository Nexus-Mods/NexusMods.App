using NexusMods.Paths;

namespace NexusMods.Common;

/// <summary>
/// A way of creating a stream from another object. Could be an entry in an archive, lazy extracted, could be a
/// file on disk, or some network resource.
/// </summary>
public interface IStreamFactory
{
    /// <summary>
    /// Last modified time of the data behind the stream, returns DateTime.Now if unknown.
    /// </summary>
    DateTime LastModifiedUtc { get; }

    /// <summary>
    /// The Path of the stream.
    /// </summary>
    IPath Name { get; }
    
    /// <summary>
    /// The size of the stream
    /// </summary>
    Size Size { get; }
    
    /// <summary>
    /// Returns a read-only stream for the given factory
    /// </summary>
    /// <returns></returns>
    ValueTask<Stream> GetStream();
}