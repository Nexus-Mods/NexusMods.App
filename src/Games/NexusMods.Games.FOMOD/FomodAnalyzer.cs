using NexusMods.DataModel.Abstractions;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.Games.FOMOD;

public class FomodAnalyzer : IFileAnalyzer
{
    // Note: No type for .fomod because FOMODs are existing archive types listed below.
    public IEnumerable<FileType> FileTypes { get; } = new [] { FileType._7Z, FileType.RAR, FileType.RAR_NEW, FileType.ZIP };

    public IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(Stream stream, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
