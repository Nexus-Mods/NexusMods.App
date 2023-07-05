using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// Matches mods that have all the .archive files in the base folder, optionally with other documentation files.
/// </summary>
public class FolderlessModInstaller : IModInstaller
{
    private static readonly RelativePath Destination = "archive/pc/mod".ToRelativePath();

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<Cyberpunk2077>()) return Priority.None;

        return archiveFiles.All(f => Helpers.IgnoreExtensions.Contains(f.Key.Extension) || f.Key.Extension == KnownExtensions.Archive)
            ? Priority.Low
            : Priority.None;
    }

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(baseModId, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<ModInstallerResult> GetMods(
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var modFiles = archiveFiles
            .Where(kv => !Helpers.IgnoreExtensions.Contains(kv.Key.Extension))
            .Select(kv =>
            {
                var (path, file) = kv;
                return file.ToFromArchive(
                    new GamePath(GameFolderType.Game, Destination.Join(path.FileName))
                );
            });

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }
}
