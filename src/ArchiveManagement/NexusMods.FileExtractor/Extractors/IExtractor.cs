using NexusMods.Common;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Extractors;

/// <summary>
/// General purpose abstraction over an extracting library for utility.  
/// Each <see cref="IExtractor"/> can support extracting from one or more archive formats.  
/// </summary>
public interface IExtractor
{
    /// <summary>
    /// A list of all the file type signatures supported by this extractor.
    /// </summary>
    public IEnumerable<FileType> SupportedSignatures { get; }
    
    /// <summary>
    /// Returns a list of extensions supported by this extractor.
    /// </summary>
    public IEnumerable<Extension> SupportedExtensions { get; }
    
    /// <summary>
    /// Extracts maps `func` over every entry in an archive.
    /// The archive need not be extracted to disk if the archive can be decompressed on-the-fly in memory.
    /// </summary>
    /// <param name="source">The source of the incoming stream</param>
    /// <param name="func">Function to apply to each entry in the archive</param>
    /// <param name="token">Cancellation token for the process</param>
    /// <typeparam name="T">Return type</typeparam>
    /// <returns>A Dictionary of RelativePath -> Return value from `func`</returns>
    public Task<IDictionary<RelativePath, T>> ForEachEntry<T>(IStreamFactory source,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default);

    /// <summary>
    /// Unconditionally extract all files from `sFn` to a specific folder.  
    /// </summary>
    /// <param name="sFn"></param>
    /// <param name="destination"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ExtractAll(IStreamFactory sFn, AbsolutePath destination, CancellationToken token);
    
    /// <summary>
    /// Given a FileType return the priority this extractor requests.
    /// Higher priority extractors will be tried first.
    /// </summary>
    /// <param name="signatures">The file types to use when determining priority.</param>
    /// <returns>The priority associated. Higher priority means it will be tried first.</returns>
    public Priority DeterminePriority(IEnumerable<FileType> signatures);
}