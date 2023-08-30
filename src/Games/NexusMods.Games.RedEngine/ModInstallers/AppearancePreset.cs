using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class AppearancePreset : IModInstaller
{
    private static readonly RelativePath[] Paths = {
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female".ToRelativePath(),
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male".ToRelativePath()
    };


    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var modFiles = archiveFiles
            .Where(kv => kv.Key.Extension == KnownExtensions.Preset)
            .SelectMany(kv =>
            {
                var (path, file) = kv;
                return Paths.Select(relPath => file.ToFromArchive(
                    new GamePath(GameFolderType.Game, relPath.Join(path))
                ));
            }).ToArray();

        if (!modFiles.Any())
            yield break;

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }
}
