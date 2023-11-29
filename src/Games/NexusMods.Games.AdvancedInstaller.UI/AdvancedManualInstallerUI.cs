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

/// <summary>
/// Provides the UI for the Advanced Manual Installer.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AdvancedManualInstallerUI : IAdvancedInstallerHandler
{
    private readonly Lazy<LoadoutRegistry> _loadoutRegistry;

    /// <summary>
    /// Construct the UI handler for the Advanced Manual Installer.
    /// </summary>
    /// <param name="provider">Service provider required to obtain Loadout information.</param>
    public AdvancedManualInstallerUI(IServiceProvider provider)
    {
        // Delay to avoid circular dependency.
        _loadoutRegistry = new Lazy<LoadoutRegistry>(provider.GetRequiredService<LoadoutRegistry>);
    }

    /// <InheritDoc/>
    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        // Get default name of the mod for UI purposes.
        Mod? mod = null;
        if (loadoutId != LoadoutId.DefaultValue)
        {
            var loadout = _loadoutRegistry.Value.Get(loadoutId);
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
            new AdvancedInstallerWindowViewModel(modName, archiveFiles, gameInstallation.LocationsRegister,
                gameInstallation.Game.Name);

        await ShowAdvancedInstallerDialog(installerViewModel);

        return (installerViewModel.AdvancedInstallerVM.ShouldInstall,
            installerViewModel.AdvancedInstallerVM.BodyViewModel.DeploymentData);
    }

    /// <summary>
    /// Creates a modal Dialog window and shows it, then awaits for it to close.
    /// </summary>
    /// <param name="dialogVM">The View Model of the dialog to create.</param>
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
                // Create the modal dialog, parent window is required for modal.
                await view.ShowDialog(desktop.MainWindow);
            }

            tuple.tcs.SetResult();
        });

        await tcs.Task;
    }

    /// <summary>
    /// Schedule an action on the UI thread.
    /// This returns immediately, without waiting for the action to actually run.
    /// </summary>
    /// <param name="state">State data required for the execution of the action.</param>
    /// <param name="action">The action to execute.</param>
    /// <typeparam name="TState">The type of the state data.</typeparam>
    private static void OnUi<TState>(TState state, Func<TState, Task> action)
    {
        AvaloniaScheduler.Instance.ScheduleAsync(state: (state, action),
            async (_, innerState, _) => { await innerState.action(innerState.state); });
    }
}
