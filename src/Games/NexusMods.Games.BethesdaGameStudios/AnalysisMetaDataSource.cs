using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class AnalysisMetaDataSource : IFileMetadataSource
{
    public IEnumerable<Extension> Extensions => new[] { new Extension(".esp"), new Extension(".esm"), new Extension(".esl") };
    public IEnumerable<FileType> FileTypes => new[] { FileType.TES4 };
    public IEnumerable<GameDomain> Games => new[] { SkyrimSpecialEdition.StaticDomain };

#pragma warning disable CS1998
    public async IAsyncEnumerable<IModFileMetadata> GetMetadataAsync(Loadout loadout, Mod mod, AModFile file,
#pragma warning restore CS1998
        AnalyzedFile analyzedFile)
    {
        yield return new AnalysisSortData()
        {
            Masters = analyzedFile.AnalysisData.OfType<PluginAnalysisData>().First().Masters
        };
    }
}
