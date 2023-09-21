using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.Generic.FileAnalyzers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Utilities;

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
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {

        var filtered = archiveFiles.GetAllDescendentFiles()
            .Where(f => !IgnoreFiles.Contains(f.Path.FileName))
            .ToList();

        // We only support ini files for now
        if (filtered.Any(f => f.Path.Extension != Ini))
            return NoResults;

        // Get all the ini data
        var iniData = await filtered
            .SelectAsync(async f => await IniAnalzyer.AnalyzeAsync(f.Value!.StreamFactory))
            .ToListAsync(cancellationToken: cancellationToken);

        // All the files must have ini data
        if (iniData.Count != filtered.Count)
            return NoResults;

        // All the files must have a section that ends with .fx marking them as likely a reshade preset
        if (!iniData.All(f => f.Sections.All(x => x.EndsWith(".fx", StringComparison.CurrentCultureIgnoreCase))))
            return NoResults;

        var modFiles = archiveFiles.GetAllDescendentFiles()
            .Where(kv => !IgnoreFiles.Contains(kv.Name.FileName))
            .Select(kv =>
            {
                var (path, file) = kv;
                return file!.ToFromArchive(
                    new GamePath(LocationId.Game, path.FileName)
                );
            }).ToArray();

        if (!modFiles.Any())
            return NoResults;

        return new [] { new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        }};
    }


}
