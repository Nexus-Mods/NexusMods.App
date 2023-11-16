using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Overlays;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

// Temporary.
#pragma warning disable CS1998

/// <summary>
///     Provides the implementation of the 'Advanced Installer' functionality.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AdvancedManualInstallerUI: IAdvancedInstallerHandler
{
    private readonly Lazy<IOverlayController> _overlayController;
    private readonly Lazy<LoadoutRegistry> _loadoutRegistry;
    private readonly IServiceProvider _provider;

    public AdvancedManualInstallerUI(IServiceProvider provider)
    {
        _provider = provider;

        // Delay to avoid circular dependency.
        _overlayController = new Lazy<IOverlayController>(provider.GetRequiredService<IOverlayController>);
        _loadoutRegistry = new Lazy<LoadoutRegistry>(provider.GetRequiredService<LoadoutRegistry>);
    }

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        // Get default name of the mod for UI purposes.
        Loadout? loadout = null;
        Mod? mod = null;
        if (loadoutId != LoadoutId.Null)
        {
            loadout = _loadoutRegistry.Value.Get(loadoutId);
            loadout?.Mods.TryGetValue(baseModId, out mod);
        }

        var modName = mod?.Name ?? Language.AdvancedInstaller_Manual_Mod;


        // Note: This code is effectively a stub.
        var (shouldInstall, deploymentData) = await GetDeploymentDataAsync(gameInstallation, loadout, modName,
            baseModId, archiveFiles, cancellationToken);

        if (!shouldInstall)
            return Array.Empty<ModInstallerResult>();

        return new[]
        {
            new ModInstallerResult
            {
                Id = baseModId,
                Files = deploymentData.EmitOperations(archiveFiles)
            }
        };
    }

    private async Task<(bool shouldInstall, DeploymentData data)> GetDeploymentDataAsync(
        GameInstallation gameInstallation, Loadout? loadout, string modName, ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken cancellationToken)
    {
        var showInstaller = await ShowUnsupportedModOverlay(modName);

        // TODO: Abort this somehow so if user closes dialog, the installed data does not change in db.
        if (!showInstaller)
            return (false, new DeploymentData());

        // This is a stub, until we implement some UI logic to pull this data
        return await ShowAdvancedInstallerOverlay(modName, archiveFiles, gameInstallation.LocationsRegister,
            gameInstallation.Game.Name);
    }

    private async Task<bool> ShowUnsupportedModOverlay(string modName, object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = new UnsupportedModOverlayViewModel(modName);
        OnUi(_overlayController.Value,
            controller => { controller.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs); });
        await tcs.Task;
        return vm.ShouldAdvancedInstall;
    }

    private async Task<(bool shouldInstall, DeploymentData data)> ShowAdvancedInstallerOverlay(string modName,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register,
        string gameName = "", object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = new AdvancedInstallerOverlayViewModel(modName, archiveFiles, register, gameName);
        OnUi(_overlayController.Value,
            controller => { controller.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs); });
        await tcs.Task;
        return (!vm.WasCancelled, vm.BodyViewModel.DeploymentData);
    }

    private static void OnUi<TState>(TState state, Action<TState> action)
    {
        // NOTE: AvaloniaScheduler has to be used to do work on the UI thread
        AvaloniaScheduler.Instance.Schedule(
            (action, state),
            AvaloniaScheduler.Instance.Now,
            (_, tuple) =>
            {
                var (innerAction, innerState) = tuple;
                innerAction(innerState);
                return Disposable.Empty;
            });
    }
}
