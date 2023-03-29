using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class AppearancePreset : IModInstaller
{
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<Cyberpunk2077>()) return Common.Priority.None;

        if (files.All(f => Helpers.IgnoreExtensions.Contains(f.Key.Extension) ||
                           (f.Value.FileTypes.Contains(FileType.Cyberpunk2077AppearancePreset) &&
                            f.Key.Extension == KnownExtensions.Preset)))
            return Common.Priority.Normal;

        return Common.Priority.None;
    }

    private RelativePath[] _paths = {
        @"bin\x64\plugins\cyber_engine_tweaks\mods\AppearanceChangeUnlocker\character-preset\female".ToRelativePath(),
        @"bin\x64\plugins\cyber_engine_tweaks\mods\AppearanceChangeUnlocker\character-preset\male".ToRelativePath()
    };

    public IEnumerable<AModFile> GetFilesToExtract(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        foreach (var (path, file) in files.Where(f => f.Key.Extension == KnownExtensions.Preset))
        {
            foreach (var relPath in _paths)
            {
                yield return new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchive, path),
                    To = new GamePath(GameFolderType.Game, relPath.Join(path.FileName)),
                    Hash = file.Hash,
                    Size = file.Size
                };
            }
        }
    }
}
