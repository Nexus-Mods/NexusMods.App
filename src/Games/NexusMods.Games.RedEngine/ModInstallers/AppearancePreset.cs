using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
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
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var modFiles = archiveFiles.GetAllDescendentFiles()
            .Where(kv => kv.Path.Extension == KnownExtensions.Preset)
            .SelectMany(kv =>
            {
                var (path, file) = kv;
                return Paths.Select(relPath => file!.ToFromArchive(
                    new GamePath(GameFolderType.Game, relPath.Join(path))
                ));
            }).ToArray();

        if (!modFiles.Any())
            return NoResults;

        return new ModInstallerResult[] { new()
        {
            Id = baseModId,
            Files = modFiles
        }};
    }
}
