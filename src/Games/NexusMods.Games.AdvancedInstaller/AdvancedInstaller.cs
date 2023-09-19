using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller;

public class AdvancedInstaller : IModInstaller
{
    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation gameInstallation, ModId baseModId, FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
