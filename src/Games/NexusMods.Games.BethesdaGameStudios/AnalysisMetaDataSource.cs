using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class AnalysisMetaDataSource : IFileMetadataSource
{
    public IEnumerable<Extension> Extensions => new[] { new Extension(".esp"), new Extension(".esm"), new Extension(".esl") };
    public IEnumerable<FileType> FileTypes => new[] { FileType.TES4 };
    public IEnumerable<GameDomain> Games => new[] { SkyrimSpecialEdition.StaticDomain };

    public async IAsyncEnumerable<IMetadata> GetMetadataAsync(
        Loadout loadout,
        Mod mod,
        AModFile file,
        AnalyzedFile analyzedFile)
    {
        await Task.Yield();
        yield return analyzedFile.AnalysisData.OfType<PluginAnalysisData>().First();
    }
}
