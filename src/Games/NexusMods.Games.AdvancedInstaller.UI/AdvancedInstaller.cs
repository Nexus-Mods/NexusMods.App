using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Overlays;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

// Temporary.
#pragma warning disable CS1998

/// <summary>
///     Provides the implementation of the 'Advanced Installer' functionality.
/// </summary>
/// <typeparam name="TUnsupportedOverlayFactory">Use <see cref="UnsupportedModOverlayViewModelFactory"/>, or alternative for testing.</typeparam>
/// <typeparam name="TAdvancedInstallerOverlayViewModelFactory">Use <see cref="AdvancedInstallerOverlayViewModelFactory"/>, or alternative for testing.</typeparam>
public class AdvancedInstaller<TUnsupportedOverlayFactory, TAdvancedInstallerOverlayViewModelFactory> : IModInstaller
    where TUnsupportedOverlayFactory : IUnsupportedModOverlayViewModelFactory
    where TAdvancedInstallerOverlayViewModelFactory : IAdvancedInstallerOverlayViewModelFactory
{
    private readonly IOverlayController _overlayController;
    private readonly Lazy<LoadoutRegistry> _loadoutRegistry;
    private readonly IServiceProvider _provider;

    public static AdvancedInstaller<UnsupportedModOverlayViewModelFactory, AdvancedInstallerOverlayViewModelFactory>
        Create(IServiceProvider provider) =>
        new(provider.GetRequiredService<IOverlayController>(), provider);

    public AdvancedInstaller(IOverlayController overlayController, IServiceProvider provider)
    {
        _overlayController = overlayController;
        _provider = provider;
        // Delay to avoid circular dependency.
        _loadoutRegistry = new Lazy<LoadoutRegistry>(() => provider.GetRequiredService<LoadoutRegistry>());
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

        var modName = mod?.Name ?? "Manual Mod";


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
        var vm = TUnsupportedOverlayFactory.Create(modName);
        OnUi(_overlayController,
            controller => { controller.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs); });
        await tcs.Task;
        return vm.ShouldAdvancedInstall;
    }

    private async Task<(bool shouldInstall, DeploymentData data)> ShowAdvancedInstallerOverlay(string modName,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register,
        string gameName = "", object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = TAdvancedInstallerOverlayViewModelFactory.Create(archiveFiles, register, gameName, modName);
        OnUi(_overlayController,
            controller => { _overlayController.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs); });
        await tcs.Task;
        return (!vm.WasCancelled, vm.BodyViewModel.Data);
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

/// <summary>
///     Factory for creating instances of <see cref="IUnsupportedModOverlayViewModel" />.
/// </summary>
public interface IUnsupportedModOverlayViewModelFactory
{
    static abstract IUnsupportedModOverlayViewModel Create(string modName = "Manual Mod");
}

public class UnsupportedModOverlayViewModelFactory : IUnsupportedModOverlayViewModelFactory
{
    public static IUnsupportedModOverlayViewModel Create(string modName = "Manual Mod")
    {
        var overlay = new UnsupportedModOverlayViewModel(modName);
        return overlay;
    }
}

/// <summary>
///     Factory for creating instances of <see cref="IAdvancedInstallerOverlayViewModel" />.
/// </summary>
public interface IAdvancedInstallerOverlayViewModelFactory
{
    static abstract IAdvancedInstallerOverlayViewModel Create(
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register,
        string gameName = "",
        string modName = "Manual Mod"
        );
}

public class AdvancedInstallerOverlayViewModelFactory : IAdvancedInstallerOverlayViewModelFactory
{
    public static IAdvancedInstallerOverlayViewModel Create(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register,
        string gameName = "", string modName = "Manual Mod")
    {
        var overlay = new AdvancedInstallerOverlayViewModel(modName, archiveFiles, register, gameName);
        return overlay;
    }
}
