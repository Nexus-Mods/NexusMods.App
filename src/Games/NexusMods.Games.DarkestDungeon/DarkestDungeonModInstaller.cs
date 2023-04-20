using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.DarkestDungeon;

public class DarkestDungeonModInstaller : IModInstaller
{
    private static readonly RelativePath ModFilesTxt = "modfiles.txt".ToRelativePath();
    private static readonly RelativePath ModFolder = "mods".ToRelativePath();

    private readonly IDataStore _dataStore;
    
    public DarkestDungeonModInstaller(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }
    
    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (installation.Game is not DarkestDungeon)
            return Common.Priority.None;

        return archiveFiles.Keys.Any(f => f.FileName == ModFilesTxt)
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
        var modFolder = archiveFiles.Keys
            .First(m => m.FileName == ModFilesTxt)
            .Parent;

        var modFiles = archiveFiles
            .Where(kv => kv.Key.InFolder(modFolder))
            .Select(kv =>
            {
                var (path, file) = kv;

                return new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchiveHash, path),
                    To = new GamePath(GameFolderType.Game, ModFolder.Join(path)),
                    Hash = file.Hash,
                    Size = file.Size
                };
            });

        yield return baseMod with
        {
            Files = modFiles.ToEntityDictionary(_dataStore)
        };
    }
}
