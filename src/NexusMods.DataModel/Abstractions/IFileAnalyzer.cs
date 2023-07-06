using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

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
    /// The unique identifier for this file analyzer, includes a revision number that
    /// should be updated whenever changes to the analyzer necessitate a re-analysis
    /// of archive files.
    /// </summary>
    public FileAnalyzerId Id { get; }

    /// <summary>
    /// Defines the file types supported by this file analyzer.
    /// </summary>
    public IEnumerable<FileType> FileTypes { get; }

    /// <summary>
    /// Asynchronously analyzes a file with the given information.
    /// </summary>
    /// <param name="info">Information about the item to analyze.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>Analysis data for the processed files.</returns>
    public IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, CancellationToken token = default);
}

/// <summary>
/// Extra info for file analyzer. Passed as a struct to avoid having to update all callees.
/// </summary>
public struct FileAnalyzerInfo
{
    /// <summary>
    /// Name of the file being analyzed.
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// Provides access to the underlying file.
    /// </summary>
    public Stream Stream { get; init; }

    /// <summary>
    /// Path to the extracted parent archive.
    /// If this item is a child of an archive file, this will be non null.
    /// </summary>
    public TemporaryPath? ParentArchive;

    /// <summary>
    /// Relative path of the file to the parent archive, if sourced from an archive.
    /// This path is empty if not sourced from an archive.
    /// </summary>
    public RelativePath? RelativePath;
}
