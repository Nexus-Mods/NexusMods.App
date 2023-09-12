using System.Diagnostics.CodeAnalysis;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.Sifu;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class SifuModInstaller : AModInstaller
{
    private static readonly Extension PakExt = new(".pak");
    private static readonly RelativePath ModsPath = "Content/Paks/~mods".ToRelativePath();

    public SifuModInstaller(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var pakFile = archiveFiles.GetAllDescendentFiles()
            .FirstOrDefault(node => node.Path.Extension == PakExt);

        if (pakFile == null)
            return NoResults;

        var pakPath = pakFile.Parent;

        var modFiles = pakPath.GetAllDescendentFiles()
            .Select(kv =>
            {
                var (path, file) = kv;
                return file!.ToFromArchive(
                    new GamePath(GameFolderType.Game, ModsPath.Join(path.RelativeTo(pakPath.Path)))
                );
            });

        return new [] { new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles,
            Name = pakPath.Name
        }};
    }

}
