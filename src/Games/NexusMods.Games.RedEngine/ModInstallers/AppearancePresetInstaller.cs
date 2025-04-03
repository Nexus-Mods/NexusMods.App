using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// This mod installer is used to install appearance presets for Cyberpunk 2077, they are installed into a specific
/// folder under the cyber engine tweaks mod's subfolder for the appearance change unlocker.
/// </summary>
public class AppearancePresetInstaller : ALibraryArchiveInstaller
{
    private static readonly RelativePath[] Paths = {
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female",
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male"
    };

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public AppearancePresetInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<AppearancePresetInstaller>>())
    {
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
