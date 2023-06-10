using NexusMods.DataModel.Abstractions;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.Games.TestFramework;

public record ModInstallerExampleFile
{
    public ulong Hash { get; init; }
    public string Name { get; init; } = string.Empty;
    public FileType[] Filetypes { get; init; } = Array.Empty<FileType>();
    public IFileAnalysisData[] AnalysisData { get; init; } = Array.Empty<IFileAnalysisData>();
};
