using NexusMods.DataModel.Abstractions;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.FOMOD;

public class FomodAnalyzer : IFileAnalyzer
{
    // Note: No type for .fomod because FOMODs are existing archive types listed below.
    public IEnumerable<FileType> FileTypes { get; } = new [] { FileType.XML };

    public IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, CancellationToken ct = default)
    {
        // We need to detect if this is a FOMOD, without the file name, somehow.

    }
}
