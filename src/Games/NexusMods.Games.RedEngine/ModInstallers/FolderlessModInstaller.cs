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
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// Matches mods that have all the .archive files in the base folder, optionally with other documentation files.
/// </summary>
public class FolderlessModInstaller : IModInstaller
{

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<Cyberpunk2077>())
            return Common.Priority.None;

        if (!installation.Is<Cyberpunk2077>()) return Common.Priority.None;

        if (files.All(f => Helpers.IgnoreExtensions.Contains(f.Key.Extension) ||
                           f.Key.Extension == KnownExtensions.Archive))
            return Common.Priority.Low;
        return Common.Priority.None;
    }

    public ValueTask<IEnumerable<AModFile>> InstallAsync(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files, CancellationToken token)
    {
        return ValueTask.FromResult(InstallImpl(srcArchive, files));
    }

    private IEnumerable<AModFile> InstallImpl(Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        foreach (var (path, file) in files)
        {
            if (Helpers.IgnoreExtensions.Contains(path.Extension))
                continue;

            yield return new FromArchive
            {
                Id = ModFileId.New(),
                From = new HashRelativePath(srcArchive, path),
                To = new GamePath(GameFolderType.Game, @"archive\pc\mod\".ToRelativePath().Join(path.FileName)),
                Hash = file.Hash,
                Size = file.Size
            };
        }
    }
}
