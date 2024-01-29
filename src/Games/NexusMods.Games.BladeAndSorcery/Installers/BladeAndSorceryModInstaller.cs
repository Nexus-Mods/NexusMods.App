using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.BladeAndSorcery.Models;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

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
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var modDirectories = info.ArchiveFiles
            .Children()
            .Select(x => x.Value)
            .Where(x => x.IsDirectory())
            .AsEnumerable();

        var mods = await GetContainedModsAsync(modDirectories)
            .ToListAsync(cancellationToken);

        return PrepareResults(info, mods);
    }

    private IEnumerable<ModInstallerResult> PrepareResults(
        ModInstallerInfo info,
        ICollection<(KeyedBox<RelativePath, ModFileTree>, ModManifest)> mods)
    {
        foreach (var (modRoot, manifest) in mods)
        {
            var modRootFolder = Constants.ModsDirectory.Join(modRoot.FileName());
            var modFileData = modRoot
                .GetFiles()
                .Select(kv => kv.ToStoredFile(
                    new GamePath(LocationId.Game, modRootFolder.Join(kv.Path().DropFirst()))
                ))
                .AsEnumerable();

            if (info.Version.ToString() != manifest.GameVersion)
                _logger.LogDebug(
                    "Mod {ModName} v{ModVersion} specifies game version {ModGameVersion}, but {CurrentGameVersion} is installed",
                    manifest.Name, manifest.ModVersion, manifest.GameVersion, info.Version);

            yield return new ModInstallerResult
            {
                Id = mods.Count == 1
                    ? info.BaseModId
                    : ModId.NewId(),
                Files = modFileData,
                Name = manifest.Name,
                Version = manifest.ModVersion
            };
        }
    }

    private static async IAsyncEnumerable<(KeyedBox<RelativePath, ModFileTree>, ModManifest)>
        GetContainedModsAsync(IEnumerable<KeyedBox<RelativePath, ModFileTree>> modDirectories)
    {
        foreach (var directory in modDirectories)
        {
            var manifestNode = directory.Children().Values.FirstOrDefault(c => c.FileName() == Constants.ModManifestFileName);
            if (manifestNode == null)
                continue;

            await using var stream = await manifestNode.Item.OpenAsync();
            var modManifest = await JsonSerializer.DeserializeAsync<ModManifest>(stream);

            if (modManifest is null)
                continue;

            yield return (manifestNode.Parent()!, modManifest);
        }
    }
}
