using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Games.Sifu;

public class SifuModInstaller : IModInstaller
{
    private Extension PAK_EXT = new Extension(".pak");
    private RelativePath _modsPath = @"Content\Paks\~mods".ToRelativePath();

    public ValueTask<IEnumerable<AModFile>> GetFilesToExtractAsync(GameInstallation installation, Hash srcArchiveHash, EntityDictionary<RelativePath, AnalyzedFile> files, CancellationToken ct = default)
    {
        return ValueTask.FromResult(GetFilesToExtractSync(srcArchiveHash, files));
    }

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return (installation.Game is Sifu) && ContainsUEModFile(files)
            ? Common.Priority.Normal
            : Common.Priority.None;
    }

    private bool ContainsUEModFile(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return !files.FirstOrDefault(kv => kv.Key.Extension == PAK_EXT)
            .Equals(default(KeyValuePair<RelativePath, AnalyzedFile>));
    }

    private IEnumerable<AModFile> GetFilesToExtractSync(Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var pakPath = files.Keys.First(filePath => filePath.FileName.Extension == PAK_EXT).Parent;

        return files
            .Where(file => file.Key.InFolder(pakPath))
            .Select(file =>
                new FromArchive
                {
                    Id = ModFileId.New(),
                    To = new GamePath(GameFolderType.Game, _modsPath.Join(file.Key.RelativeTo(pakPath))),
                    From = new HashRelativePath(srcArchive, file.Key),
                    Hash = file.Value.Hash,
                    Size = file.Value.Size
                });
    }
}
