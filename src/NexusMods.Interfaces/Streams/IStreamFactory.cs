using NexusMods.Paths;

namespace NexusMods.Interfaces.Streams;

/// <summary>
///     A generic way of specifying a file-like source. Could be a in memory object
///     a file on disk, or a file inside an archive.
/// </summary>
public interface IStreamFactory
{
    DateTime LastModifiedUtc { get; }

    IPath Name { get; }
    
    Size Size { get; }
    
    ValueTask<Stream> GetStream();
    
}