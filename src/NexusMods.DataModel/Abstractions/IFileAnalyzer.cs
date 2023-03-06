using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Provides an abstraction over a component used to analyze files.<br/><br/>
///
/// A file analyzer reads data from a given stream, and extracts metadata necessary
/// for. Such as dependency information.
/// </summary>
public interface IFileAnalyzer
{
    /// <summary>
    /// Defines the file types supported by this file analyzer.
    /// </summary>
    public IEnumerable<FileType> FileTypes { get; }

    /// <summary>
    /// Asynchronously analyzes a file exposed by a given stream.
    /// </summary>
    /// <param name="stream">The stream to be analyzed; be it a file or an archive.</param>
    /// <param name="ct">Allows you to cancel the operation.</param>
    /// <returns>Analysis data for the processed files.</returns>
    public IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(Stream stream, CancellationToken ct = default);
}
