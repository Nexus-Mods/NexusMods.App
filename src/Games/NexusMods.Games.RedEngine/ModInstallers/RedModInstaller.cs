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

    private readonly IDataStore _dataStore;

    public RedModInstaller(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<Cyberpunk2077>()) return Common.Priority.None;

        return archiveFiles.Any(IsInfoJson) ? Common.Priority.High : Common.Priority.None;
    }

    private static bool IsInfoJson(KeyValuePair<RelativePath, AnalyzedFile> file)
    {
        return file.Key.FileName == InfoJson && file.Value.AnalysisData.OfType<RedModInfo>().Any();
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

                        return new FromArchive
                        {
                            Id = ModFileId.New(),
                            From = new HashRelativePath(srcArchiveHash, path),
                            To = new GamePath(GameFolderType.Game, Mods.Join(parentName).Join(path.RelativeTo(parent))),
                            Hash = file.Hash,
                            Size = file.Size
                        };
                    });
            });

        yield return baseMod with
        {
            Files = modFiles.ToEntityDictionary(_dataStore)
        };
    }
}
