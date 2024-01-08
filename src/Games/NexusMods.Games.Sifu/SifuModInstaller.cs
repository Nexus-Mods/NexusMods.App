using System.Diagnostics.CodeAnalysis;
using NexusMods.DataModel.Abstractions.Games;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Trees;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

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
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var pakFile = archiveFiles.GetFiles()
            .FirstOrDefault(node => node.Path().Extension == PakExt);

        if (pakFile == null)
            return NoResults;

        var pakPath = pakFile.Parent();
        var modFiles = pakPath!.GetFiles()
            .Select(kv => kv.ToStoredFile(
                new GamePath(LocationId.Game, ModsPath.Join(kv.Path().RelativeTo(pakPath!.Path())))
            ));

        return new [] { new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles,
            Name = pakPath!.FileName()
        }};
    }

}
