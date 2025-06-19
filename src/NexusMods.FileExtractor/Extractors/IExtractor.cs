using NexusMods.Abstractions.FileExtractor;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.FileExtractor.Extractors;

/// <summary>
/// Generic enum for expressing the priority of a given operation. Most operations will be Normal priority, but
/// with more specific operations being given higher priority, and those that should be used as a last resort being
/// given lower priority. Use Priority.None for operations that should not be used at all.
/// </summary>
public enum Priority
{
    Highest = 0,
    High,
    Normal,
    Low,
    Lowest,
    None = int.MaxValue
}

/// <summary>
/// General purpose abstraction over an extracting library for utility.
/// Each <see cref="IExtractor"/> can support extracting from one or more archive formats.
/// </summary>
public interface IExtractor
{
    /// <summary>
    /// A list of all the file type signatures supported by this extractor.
    /// </summary>
    public FileType[] SupportedSignatures { get; }

    /// <summary>
    /// Returns a list of extensions supported by this extractor.
    /// </summary>
    public Extension[] SupportedExtensions { get; }

    /// <summary>
    /// Unconditionally extract all files from `sFn` to a specific folder.
    /// </summary>
    /// <param name="source">The source of the incoming stream</param>
    /// <param name="destination">Where the files are to be extracted</param>
    /// <param name="token">Token used for cancellation of the task</param>
    /// <returns>Task signalling completion.</returns>
    Task ExtractAllAsync(IStreamFactory source, AbsolutePath destination, CancellationToken token = default);

    /// <summary>
    /// Given a FileType return the priority this extractor requests.
    /// Higher priority extractors will be tried first.
    /// </summary>
    /// <param name="signatures">The file types to use when determining priority.</param>
    /// <returns>The priority associated. Higher priority means it will be tried first.</returns>
    public Priority DeterminePriority(IEnumerable<FileType> signatures);
}
