using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.DataModel.Abstractions;

public interface IFileAnalyzer
{
    public IEnumerable<FileType> FileTypes { get; }
    public IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(Stream stream, CancellationToken ct = default);
}
