using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.RedEngine.FileAnalyzers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class RedModInstaller : IModInstaller
{
    private static readonly RelativePath InfoJson = "info.json".ToRelativePath();
    private static readonly RelativePath Mods = "mods".ToRelativePath();

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<Cyberpunk2077>()) return Priority.None;

        return archiveFiles.Any(IsInfoJson) ? Priority.High : Priority.None;
    }

    private static bool IsInfoJson(KeyValuePair<RelativePath, AnalyzedFile> file)
    {
        return file.Key.FileName == InfoJson && file.Value.AnalysisData.OfType<RedModInfo>().Any();
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
            .Where(IsInfoJson)
            .SelectMany(infoJson =>
            {
                var parent = infoJson.Key.Parent;
                var parentName = parent.FileName;

                return archiveFiles
                    .Where(kv => kv.Key.InFolder(parent))
                    .Select(kv =>
                    {
                        var (path, file) = kv;
                        return file.ToFromArchive(
                            new GamePath(GameFolderType.Game, Mods.Join(parentName).Join(path.RelativeTo(parent)))
                        );
                    });
            });

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }
}
