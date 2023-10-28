using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
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
///     Provides the implementation of the 'Advanced Installer' functionality.
/// </summary>
/// <typeparam name="TUnsupportedOverlayFactory">Use <see cref="UnsupportedModOverlayViewModelFactory"/>, or alternative for testing.</typeparam>
/// <typeparam name="TAdvancedInstallerOverlayViewModelFactory">Use <see cref="AdvancedInstallerOverlayViewModelFactory"/>, or alternative for testing.</typeparam>
public class AdvancedInstaller<TUnsupportedOverlayFactory, TAdvancedInstallerOverlayViewModelFactory> : IModInstaller
    where TUnsupportedOverlayFactory : IUnsupportedModOverlayViewModelFactory
    where TAdvancedInstallerOverlayViewModelFactory : IAdvancedInstallerOverlayViewModelFactory
{
    private readonly IOverlayController _overlayController;

    public static AdvancedInstaller<UnsupportedModOverlayViewModelFactory, AdvancedInstallerOverlayViewModelFactory> Create(IServiceProvider provider) =>
        new(provider.GetRequiredService<IOverlayController>());

    public AdvancedInstaller(IOverlayController overlayController)
    {
        _overlayController = overlayController;
    }

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation gameInstallation,
        ModId baseModId, FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
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

    private async Task<DeploymentData> GetDeploymentDataAsync(GameInstallation gameInstallation, ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken cancellationToken)
    {
        var showInstaller = await ShowUnsupportedModOverlay();

        // TODO: Abort this somehow so if user closes dialog, the installed data does not change in db.
        if (!showInstaller)
            return new DeploymentData();

        // This is a stub, until we implement some UI logic to pull this data
        return await ShowAdvancedInstallerOverlay(archiveFiles, gameInstallation.LocationsRegister,
            gameInstallation.Game.Name);
    }

    private async Task<bool> ShowUnsupportedModOverlay(object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = TUnsupportedOverlayFactory.Create();
        OnUi(_overlayController, controller =>
        {
            controller.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs);
        });
        await tcs.Task;
        return vm.ShouldAdvancedInstall;
    }

    private async Task<DeploymentData> ShowAdvancedInstallerOverlay(
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register,
        string gameName = "", object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = TAdvancedInstallerOverlayViewModelFactory.Create(archiveFiles, register, gameName);
        OnUi(_overlayController, controller =>
        {
            _overlayController.SetOverlayContent(new SetOverlayItem(vm, referenceItem), tcs);
        });
        await tcs.Task;
        return vm.BodyViewModel.Data;
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
    static abstract IUnsupportedModOverlayViewModel Create();
}

public class UnsupportedModOverlayViewModelFactory : IUnsupportedModOverlayViewModelFactory
{
    public static IUnsupportedModOverlayViewModel Create() => new UnsupportedModOverlayViewModel();
}

/// <summary>
///     Factory for creating instances of <see cref="IAdvancedInstallerOverlayViewModel" />.
/// </summary>
public interface IAdvancedInstallerOverlayViewModelFactory
{
    static abstract IAdvancedInstallerOverlayViewModel Create(
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register,
        string gameName = "");
}

public class AdvancedInstallerOverlayViewModelFactory : IAdvancedInstallerOverlayViewModelFactory
{
    public static IAdvancedInstallerOverlayViewModel Create(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register,
        string gameName = "") =>
        new AdvancedInstallerOverlayViewModel(archiveFiles, register, gameName);
}
