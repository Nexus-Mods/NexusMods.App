using Cathei.LinqGen;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Extensions.BCL;
using NexusMods.Games.Generic.FileAnalyzers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.Reshade;

public class ReshadePresetInstaller : AModInstaller
{
    private static readonly HashSet<RelativePath> IgnoreFiles = new[]
        {
            "readme.txt",
            "installation.txt",
            "license.txt"
        }
        .Select(t => t.ToRelativePath())
        .ToHashSet();

    private static Extension Ini = new(".ini");

    public ReshadePresetInstaller(IServiceProvider serviceProvider) : base(serviceProvider) {}

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var filtered = info.ArchiveFiles.GetFiles()
            .Gen()
            .Where(f => !IgnoreFiles.Contains(f.Path().FileName))
            .ToList();

        // We only support ini files for now
        if (filtered.Any(f => f.FileName().Extension != Ini))
            return NoResults;

        // Get all the ini data
        var iniData = await filtered
            .SelectAsync(async f => await IniAnalzyer.AnalyzeAsync(f.Item.StreamFactory!))
            .ToListAsync(cancellationToken: cancellationToken);

        // All the files must have ini data
        if (iniData.Count != filtered.Count)
            return NoResults;

        // All the files must have a section that ends with .fx marking them as likely a reshade preset
        if (!iniData.All(f => f.Sections.All(x => x.EndsWith(".fx", StringComparison.CurrentCultureIgnoreCase))))
            return NoResults;

        var modFiles = info.ArchiveFiles.GetFiles()
            .Gen()
            .Where(kv => !IgnoreFiles.Contains(kv.FileName()))
            .Select(kv => kv.ToStoredFile(
                new GamePath(LocationId.Game, kv.FileName())
            )).ToArray();

        if (modFiles.Length == 0)
            return NoResults;

        return new [] { new ModInstallerResult
        {
            Id = info.BaseModId,
            Files = modFiles
        }};
    }
}
