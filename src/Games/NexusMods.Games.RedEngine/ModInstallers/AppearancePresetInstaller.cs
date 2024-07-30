using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// This mod installer is used to install appearance presets for Cyberpunk 2077, they are installed into a specific
/// folder under the cyber engine tweaks mod's subfolder for the appearance change unlocker.
/// </summary>
public class AppearancePresetInstaller : ALibraryArchiveInstaller, IModInstaller
{
    private static readonly RelativePath[] Paths = {
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female".ToRelativePath(),
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male".ToRelativePath()
    };

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public AppearancePresetInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<AppearancePresetInstaller>>())
    {
    }

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var modFiles = info.ArchiveFiles.GetFiles()
            .Where(kv => kv.Path().Extension == new Extension(".preset"))
            .SelectMany(kv => Paths.Select(relPath => kv.ToStoredFile(
                new GamePath(LocationId.Game, relPath.Join(kv.Path()))
            ))).ToArray();

        if (!modFiles.Any())
            return [];

        return new ModInstallerResult[] { new()
        {
            Id = info.BaseModId,
            Files = modFiles,
        }};
    }

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction tx,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var tree = libraryArchive.GetTree();
        var extensionPreset = new Extension(".preset");

        var modFiles = tree.GetFiles()
            .Where(kv => kv.Key().Extension == extensionPreset)
            .SelectMany(kv => Paths.Select(relPath => kv.ToLoadoutFile(
                loadout.Id,
                loadoutGroup.Id,
                tx,
                new GamePath(LocationId.Game, relPath.Join(kv.Key()))
            ))).ToArray();

        return modFiles.Length == 0
            ? ValueTask.FromResult<InstallerResult>(new NotSupported())
            : ValueTask.FromResult<InstallerResult>(new Success());
    }
}
