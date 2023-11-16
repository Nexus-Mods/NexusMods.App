using System.Reactive.Concurrency;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
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
public class AdvancedManualInstallerUI : IAdvancedInstallerHandler
{
    private readonly Lazy<LoadoutRegistry> _loadoutRegistry;

    public AdvancedManualInstallerUI(IServiceProvider provider)
    {
        // Delay to avoid circular dependency.
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
        var (shouldInstall, deploymentData) = await GetDeploymentDataAsync(gameInstallation, modName,
            archiveFiles);

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
        GameInstallation gameInstallation, string modName,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles)
    {
        var installerViewModel =
            new AdvancedInstallerWindowViewModel(modName, archiveFiles, gameInstallation.LocationsRegister);

        await ShowAdvancedInstallerDialog(installerViewModel);

        return (installerViewModel.AdvancedInstallerVM.ShouldInstall,
            installerViewModel.AdvancedInstallerVM.BodyViewModel.DeploymentData);
    }

    private static async Task ShowAdvancedInstallerDialog(IAdvancedInstallerWindowViewModel dialogVM)
    {
        var tcs = new TaskCompletionSource();

        OnUi((dialogVM, tcs), async tuple =>
        {
            var view = new AdvancedInstallerWindowView
            {
                ViewModel = tuple.dialogVM
            };

            // Get the main window.
            if (Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            {
                await view.ShowDialog(desktop.MainWindow);
            }

            tcs.SetResult();
        });

        await tcs.Task;
    }

    private static void OnUi<TState>(TState state, Func<TState, Task> action)
    {
        AvaloniaScheduler.Instance.ScheduleAsync(state: (state, action),
            async (_, innerState, _) => { await innerState.action(innerState.state); });
    }

}
