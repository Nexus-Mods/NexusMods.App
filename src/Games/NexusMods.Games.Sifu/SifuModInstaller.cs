using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
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
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var pakFile = info.ArchiveFiles.GetFiles()
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
            Files = modFiles,
            Name = pakPath!.FileName(),
        }};
    }
}
