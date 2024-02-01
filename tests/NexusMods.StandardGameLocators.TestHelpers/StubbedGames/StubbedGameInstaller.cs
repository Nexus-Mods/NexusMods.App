using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGameInstaller : IModInstaller
{
    private readonly RelativePath _preferencesPrefix = "preferences".ToRelativePath();
    private readonly RelativePath _savesPrefix = "saves".ToRelativePath();

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(info));
    }

    private IEnumerable<ModInstallerResult> GetMods(ModInstallerInfo info)
    {
        var modFiles = info.ArchiveFiles.GetFiles()
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
            Id = info.BaseModId,
            Files = modFiles.AsEnumerable()
        };
    }
}
