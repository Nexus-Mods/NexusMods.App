using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Extractors;

/// <summary>
/// General purpose abstraction over an extracting library for utility.
/// Each <see cref="IExtractor"/> can support extracting from one or more archive formats.
/// </summary>
public interface IExtractor
{
    /// <summary>
    /// The activity group for activities created by extractors.
    /// </summary>
    public static readonly ActivityGroup Group = ActivityGroup.From("FileExtractor");

    /// <summary>
    /// A list of all the file type signatures supported by this extractor.
    /// </summary>
    public FileType[] SupportedSignatures { get; }

    /// <summary>
    /// Returns a list of extensions supported by this extractor.
    /// </summary>
    public Extension[] SupportedExtensions { get; }

    /// <summary>
    /// Extracts and calls `func` over every entry in an archive.
    /// </summary>
    /// <param name="source">The source of the incoming stream</param>
    /// <param name="func">Function to apply to each entry in the archive</param>
    /// <param name="token">Cancellation token for the process</param>
    /// <typeparam name="T">Return type</typeparam>
    /// <returns>A Dictionary of RelativePath -> Return value from `func`</returns>
    /// <remarks>
    ///     Does not extract files to disk. If you need to save the data; copy it elsewhere.
    ///     The source data passed to func can be in-memory.
    /// </remarks>
    public Task<IDictionary<RelativePath, T>> ForEachEntryAsync<T>(IStreamFactory source,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default);

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
