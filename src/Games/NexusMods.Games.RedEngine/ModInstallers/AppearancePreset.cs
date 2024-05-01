using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine.ModInstallers;

/// <summary>
/// This mod installer is used to install appearance presets for Cyberpunk 2077, they are installed into a specific
/// folder under the cyber engine tweaks mod's subfolder for the appearance change unlocker.
/// </summary>
public class AppearancePreset : AModInstaller
{
    private static readonly RelativePath[] Paths = {
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female".ToRelativePath(),
        "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male".ToRelativePath()
    };

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public AppearancePreset(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var modFiles = info.ArchiveFiles.GetFiles()
            .Where(kv => kv.Path().Extension == new Extension(".preset"))
            .SelectMany(kv => Paths.Select(relPath => kv.ToStoredFile(
                new GamePath(LocationId.Game, relPath.Join(kv.Path()))
            ))).ToArray();

        if (!modFiles.Any())
            return NoResults;

        return new ModInstallerResult[] { new()
        {
            Id = info.BaseModId,
            Files = modFiles
        }};
    }
}
