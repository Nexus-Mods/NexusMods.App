using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.BladeAndSorcery.Models;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.BladeAndSorcery.Installers;

/// <summary>
/// <see cref="IModInstaller"/> implementation for official Blade & Sorcery mods with <c>manifest.json</c> files.
/// </summary>
public class BladeAndSorceryModInstaller : AModInstaller
{
    private readonly ILogger<BladeAndSorceryModInstaller> _logger;

    public BladeAndSorceryModInstaller(IServiceProvider serviceProvider, ILogger<BladeAndSorceryModInstaller> logger)
        : base(serviceProvider)
    {
        _logger = logger;
    }

    public static BladeAndSorceryModInstaller Create(IServiceProvider serviceProvider)
        => new(serviceProvider, serviceProvider.GetRequiredService<ILogger<BladeAndSorceryModInstaller>>());

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var modDirectories = archiveFiles
            .Children
            .Select(x => x.Value)
            .Where(x => x.IsDirectory)
            .ToList();

        var mods = await GetContainedModsAsync(modDirectories)
            .ToListAsync(cancellationToken);

        return PrepareResults(gameInstallation, baseModId, mods);
    }

    private IEnumerable<ModInstallerResult> PrepareResults(
        GameInstallation gameInstallation,
        ModId baseModId,
        ICollection<(FileTreeNode<RelativePath, ModSourceFileEntry>, ModManifest)> mods)
    {
        foreach (var (modRoot, manifest) in mods)
        {
            var modRootFolder = Constants.ModsDirectory.Join(modRoot.Name);

            var modFileData = modRoot
                .GetAllDescendentFiles()
                .Select(kv =>
                {
                    var (path, file) = kv;
                    var filePath = path.DropFirst();
                    return file!.ToStoredFile(
                        new GamePath(LocationId.Game, modRootFolder.Join(filePath))
                    );
                });

            if (gameInstallation.Version.ToString() != manifest.GameVersion)
                _logger.LogDebug(
                    "Mod {ModName} v{ModVersion} specifies game version {ModGameVersion}, but {CurrentGameVersion} is installed.",
                    manifest.Name, manifest.ModVersion, manifest.GameVersion, gameInstallation.Version);

            yield return new ModInstallerResult
            {
                Id = mods.Count == 1
                    ? baseModId
                    : ModId.New(),
                Files = modFileData,
                Name = manifest.Name,
                Version = manifest.ModVersion
            };
        }
    }

    private static async IAsyncEnumerable<(FileTreeNode<RelativePath, ModSourceFileEntry>, ModManifest)>
        GetContainedModsAsync(IEnumerable<FileTreeNode<RelativePath, ModSourceFileEntry>> modDirectories)
    {
        foreach (var fileTreeNode in modDirectories
                     .Select(x => x.Children.Values.FirstOrDefault(c => c.Name == Constants.ModManifestFileName)))
        {
            if (fileTreeNode?.Value is null)
                continue;

            await using var stream = await fileTreeNode.Value.Open();

            var modManifest = await JsonSerializer.DeserializeAsync<ModManifest>(stream);

            if (modManifest is null)
                continue;

            yield return (fileTreeNode.Parent, modManifest);
        }
    }
}
