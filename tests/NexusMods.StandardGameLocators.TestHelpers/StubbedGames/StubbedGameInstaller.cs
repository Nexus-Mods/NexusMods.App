using Cathei.LinqGen;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Trees;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
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
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(loadoutId, baseModId, archiveFiles));
    }

    private IEnumerable<ModInstallerResult> GetMods(
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles)
    {
        var modFiles = archiveFiles.GetFiles()
            .Select(kv =>
            {
                var path = kv.Path();
                if (path.Path.StartsWith(_preferencesPrefix))
                    return kv.ToStoredFile(new GamePath(LocationId.Preferences, path));

                if (path.Path.StartsWith(_savesPrefix))
                    return kv.ToStoredFile(new GamePath(LocationId.Saves, path));

                return kv.ToStoredFile(new GamePath(LocationId.Game, path));
            });

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles.AsEnumerable()
        };
    }
}
