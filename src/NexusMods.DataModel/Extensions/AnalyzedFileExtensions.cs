using JetBrains.Annotations;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Paths;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extension methods for <see cref="AnalyzedFile"/>
/// </summary>
[PublicAPI]
public static class AnalyzedFileExtensions
{
    /// <summary>
    /// Maps the provided <see cref="AnalyzedFile"/> to a <see cref="GameFile"/>
    /// </summary>
    public static GameFile ToGameFile(this AnalyzedFile analyzedFile, GamePath to, GameInstallation installation)
    {
        return new GameFile
        {
            Id = ModFileId.New(),
            To = to,
            Installation = installation,
            Hash = analyzedFile.Hash,
            Size = analyzedFile.Size,
            Metadata = analyzedFile.AnalysisData.AsMetadata()
        };
    }

    /// <summary>
    /// Maps the provided <see cref="AnalyzedFile"/> to a <see cref="FromArchive"/>.
    /// </summary>
    public static FromArchive ToFromArchive(this AnalyzedFile analyzedFile, GamePath to)
    {
        return new FromArchive
        {
            Id = ModFileId.New(),
            To = to,
            Hash = analyzedFile.Hash,
            Size = analyzedFile.Size,
            Metadata = analyzedFile.AnalysisData.AsMetadata()
        };
    }
}
