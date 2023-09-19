using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller;

// Temporary.
#pragma warning disable CS1998

/// <summary>
/// Provides the implementation of the 'Advanced Installer' functionality.
/// </summary>
public class AdvancedInstaller : IModInstaller
{
    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation gameInstallation, ModId baseModId, FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        // Note: This code is effectively a stub.
        var deploymentData = await GetDeploymentDataAsync(gameInstallation, baseModId, archiveFiles, cancellationToken);
        return new[]
        {
            new ModInstallerResult
            {
                Id = baseModId,
                Files = deploymentData.EmitOperations(archiveFiles)
            }
        };
    }

    private async Task<DeploymentData> GetDeploymentDataAsync(GameInstallation gameInstallation, ModId baseModId, FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken cancellationToken)
#pragma warning restore CS1998
    {
        // This is a stub, until we implement some UI logic to pull this data
        return new DeploymentData();
    }
}
