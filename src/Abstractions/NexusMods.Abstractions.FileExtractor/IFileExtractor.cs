using NexusMods.Abstractions.IO;
using NexusMods.Paths;

namespace NexusMods.Abstractions.FileExtractor;

/// <summary>
/// Interface for a file extractor manager that can extract files somewhere
/// </summary>
public interface IFileExtractor
{
    /// <summary>
    /// Extracts all files to disk in an asynchronous fashion.
    /// </summary>
    /// <param name="path">The path to the source file.</param>
    /// <param name="dest">The folder where the file is to be extracted.</param>
    /// <param name="token">Used for cancellation of the operation.</param>
    public Task ExtractAllAsync(AbsolutePath path, AbsolutePath dest, CancellationToken token = default);

    /// <summary>
    /// Extracts all files to disk in an asynchronous fashion.
    /// </summary>
    /// <param name="sFn">The source stream.</param>
    /// <param name="dest">Destination file path.</param>
    /// <param name="token">Used for cancellation of the operation.</param>
    /// <exception cref="FileExtractionException"></exception>
    public Task ExtractAllAsync(IStreamFactory sFn, AbsolutePath dest, CancellationToken token = default);


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
    public Task<IDictionary<RelativePath, T>> ForEachEntry<T>(IStreamFactory source,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default);


    /// <summary>
    /// Tests if a specific file can be extracted with this extractor.
    /// </summary>
    /// <param name="sFn">The stream to test.</param>
    /// <returns>True if the extractor can extract this stream, else false.</returns>
    public Task<bool> CanExtract(IStreamFactory sFn);
}
