using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class AnalysisMetaDataSource : IFileMetadataSource
{
    public IEnumerable<Extension> Extensions => new []{ new Extension(".esp"), new Extension(".esm"), new Extension(".esl") };
    public IEnumerable<FileType> FileTypes => new[] { FileType.TES4 };
    public IEnumerable<string> Games => new[] { SkyrimSpecialEdition.StaticSlug };
    public async IAsyncEnumerable<IModFileMetadata> GetMetadata(Loadout filLoadout, Mod mod, AModFile file,
        AnalyzedFile analyzedFile)
    {
        yield return new AnalysisSortData()
        {
            Masters = analyzedFile.AnalysisData.OfType<PluginAnalysisData>().First().Masters
        };
    }
}