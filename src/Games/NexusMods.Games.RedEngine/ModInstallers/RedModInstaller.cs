using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
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
    private static RelativePath _infoJson = "info.json".ToRelativePath();
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<Cyberpunk2077>()) return Common.Priority.None;

        return files.Any(IsInfoJson) ? Common.Priority.High : Common.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        foreach (var infoJson in files.Where(IsInfoJson))
        {
            var parent = infoJson.Key.Parent;
            var parentName = parent.FileName;
            foreach (var (path, file) in files.Where(f => f.Key.InFolder(parent)))
            {
                yield return new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchive, path),
                    To = new GamePath(GameFolderType.Game, @"mods".ToRelativePath().Join(parentName).Join(path.RelativeTo(parent))),
                    Hash = file.Hash,
                    Size = file.Size,
                    Store = file.Store
                };
            }
        }
    }

    private bool IsInfoJson(KeyValuePair<RelativePath, AnalyzedFile> file)
    {
        return file.Key.FileName == _infoJson && file.Value.AnalysisData.OfType<RedModInfo>().Any();
    }
}