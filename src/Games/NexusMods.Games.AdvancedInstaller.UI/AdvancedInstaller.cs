using NexusMods.App.UI.Overlays;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

// Temporary.
#pragma warning disable CS1998

/// <summary>
/// Provides the implementation of the 'Advanced Installer' functionality.
/// </summary>
public class AdvancedInstaller : IModInstaller
{
    private readonly IOverlayController _overlayController;

    public AdvancedInstaller(IOverlayController overlayController)
    {
        _overlayController = overlayController;
    }

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
    {
        var showInstaller = await ShowUnsupportedModOverlay();

        // TODO: Abort this somehow so if user closes dialog, the installed data does not change in db.
        if (!showInstaller)
            return new DeploymentData();

        // This is a stub, until we implement some UI logic to pull this data
        return await ShowAdvancedInstallerOverlay(archiveFiles, gameInstallation.LocationsRegister, gameInstallation.Game.Name);
    }

    private async Task<bool> ShowUnsupportedModOverlay(object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = new UnsupportedModOverlayViewModel();
        _overlayController.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs);
        await tcs.Task;
        return vm.ShouldAdvancedInstall;
    }

    private async Task<DeploymentData> ShowAdvancedInstallerOverlay(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register, string gameName = "", object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = new AdvancedInstallerOverlayViewModel(archiveFiles, register, gameName);
        _overlayController.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs);
        await tcs.Task;
        return vm.BodyViewModel.Data;
    }
}
