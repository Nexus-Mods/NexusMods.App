using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
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
    private static readonly RelativePath[] Paths = {
        @"bin\x64\plugins\cyber_engine_tweaks\mods\AppearanceChangeUnlocker\character-preset\female".ToRelativePath(),
        @"bin\x64\plugins\cyber_engine_tweaks\mods\AppearanceChangeUnlocker\character-preset\male".ToRelativePath()
    };

    private readonly IDataStore _dataStore;

    public AppearancePreset(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<Cyberpunk2077>()) return Common.Priority.None;

        return archiveFiles.All(f => Helpers.IgnoreExtensions.Contains(f.Key.Extension) || (f.Value.FileTypes.Contains(FileType.Cyberpunk2077AppearancePreset) && f.Key.Extension == KnownExtensions.Preset))
            ? Common.Priority.Normal
            : Common.Priority.None;
    }

    public ValueTask<IEnumerable<Mod>> GetModsAsync(
        GameInstallation gameInstallation,
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(baseMod, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<Mod> GetMods(
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var modFiles = archiveFiles
            .Where(kv => kv.Key.Extension == KnownExtensions.Preset)
            .SelectMany(kv =>
            {
                var (path, file) = kv;

                return Paths.Select(relPath => new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchiveHash, path),
                    To = new GamePath(GameFolderType.Game, relPath.Join(path)),
                    Hash = file.Hash,
                    Size = file.Size
                });
            });

        yield return baseMod with
        {
            Files = modFiles.ToEntityDictionary(_dataStore)
        };
    }
}
