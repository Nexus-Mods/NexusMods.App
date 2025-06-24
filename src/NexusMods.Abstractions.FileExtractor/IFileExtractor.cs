using NexusMods.Paths;
using NexusMods.Sdk.IO;

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
    public Task ExtractAllAsync(IStreamFactory sFn, AbsolutePath dest, CancellationToken token = default);

    /// <summary>
    /// Tests if a specific file can be extracted with this extractor.
    /// </summary>
    /// <param name="path">The path to the file to test.</param>
    /// <returns>True if the extractor can extract this file, else false.</returns>
    public ValueTask<bool> CanExtract(AbsolutePath path);

    /// <summary>
    /// Tests if a specific file can be extracted with this extractor.
    /// </summary>
    /// <param name="stream">The stream to test..</param>
    /// <returns>True if the extractor can extract this file, else false.</returns>
    public ValueTask<bool> CanExtract(Stream stream);

    /// <summary>
    /// Tests if a specific file can be extracted with this extractor.
    /// </summary>
    /// <param name="sFn">The stream to test.</param>
    /// <returns>True if the extractor can extract this stream, else false.</returns>
    public ValueTask<bool> CanExtract(IStreamFactory sFn);
}
