using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

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

    public ReshadePresetInstaller(IServiceProvider serviceProvider) : base(serviceProvider) {}

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var modFiles = archiveFiles.GetAllDescendentFiles()
            .Where(kv => !IgnoreFiles.Contains(kv.Name.FileName))
            .Select(kv =>
            {
                var (path, file) = kv;
                return file!.ToFromArchive(
                    new GamePath(GameFolderType.Game, path.FileName)
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
