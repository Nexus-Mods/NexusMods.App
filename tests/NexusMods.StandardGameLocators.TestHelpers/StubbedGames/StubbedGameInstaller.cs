using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
using Hash = NexusMods.Hashing.xxHash64.Hash;

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGameInstaller : IModInstaller
{
    private readonly RelativePath _preferencesPrefix = "preferences".ToRelativePath();
    private readonly RelativePath _savesPrefix = "saves".ToRelativePath();

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(loadoutId, baseModId, archiveFiles));
    }

    private IEnumerable<ModInstallerResult> GetMods(
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles)
    {
        var modFiles = archiveFiles.GetAllDescendentFiles()
            .Select(kv =>
            {
                var (path, file) = kv;
                if (path.Path.StartsWith(_preferencesPrefix))
                {
                    return file!.ToStoredFile(
                        new GamePath(LocationId.Preferences, path)
                    );
                }

                if (path.Path.StartsWith(_savesPrefix))
                {
                    return file!.ToStoredFile(
                        new GamePath(LocationId.Saves, path)
                    );

                }

                return file!.ToStoredFile(
                    new GamePath(LocationId.Game, path));
                ;
            });

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }
}
