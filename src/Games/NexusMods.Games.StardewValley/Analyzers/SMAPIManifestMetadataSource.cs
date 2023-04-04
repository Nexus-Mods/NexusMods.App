using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace NexusMods.Games.StardewValley.Analyzers;

public class SMAPIManifestMetadataSource : IFileMetadataSource
{
    public IEnumerable<Extension> Extensions => new[] { new Extension(".json") };
    public IEnumerable<FileType> FileTypes => new[] { FileType.JSON };
    public IEnumerable<GameDomain> Games => new[] { StardewValley.GameDomain };

#pragma warning disable CS1998
    public async IAsyncEnumerable<IModFileMetadata> GetMetadataAsync(Loadout loadout, Mod mod, AModFile file, AnalyzedFile analyzedFile)
#pragma warning restore CS1998
    {
        var dependencies = analyzedFile.AnalysisData
            .OfType<SMAPIManifest>()
            .FirstOrDefault()?.Dependencies;

        if (dependencies is null) yield break;
        yield return new SMAPIDependencies
        {
            Dependencies = dependencies
        };
    }
}

[JsonName("NexusMods.Games.StardewValley.SMAPIDependencies")]
public record SMAPIDependencies : IModFileMetadata
{
    public required SMAPIManifestDependency[] Dependencies { get; init; }
}
