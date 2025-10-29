using System.Reactive.Concurrency;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;

namespace NexusMods.Games.AdvancedInstaller.UI;

/// <summary>
/// Provides the UI for the Advanced Manual Installer.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AdvancedManualInstallerUI : ALibraryArchiveInstaller, IAdvancedInstallerHandler
{
    /// <summary>
    /// If true, the UI is not running and the installer will quietly fail.
    ///
    /// NOTE(halgari): this is an ugly hack, but we don't have a better tool for it at the moment.
    /// </summary>
    public static bool Headless { get; set; } = false;

    public bool WasOpenedDirectly { get; set; }
    private readonly ILogger<AdvancedManualInstallerUI> _logger;

    /// <summary>
    /// Construct the UI handler for the Advanced Manual Installer.
    /// </summary>
    public AdvancedManualInstallerUI(IServiceProvider provider, ILogger<AdvancedManualInstallerUI> logger) : base(provider, logger) => _logger = logger;

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        Transaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (Headless) return new NotSupported(Reason: "The advanced manual installer can't be used in headless mode");
        var tree = LibraryArchiveTree.Create(libraryArchive);
        var (shouldInstall, deploymentData) = await GetDeploymentDataAsync(libraryArchive.AsLibraryFile().AsLibraryItem().Name, tree, loadout);

        // Note(sewer): Normally cancellation flows through CancellationTokenSource.Cancel() -> CancellationToken -> OperationCanceledException.
        //              Here we only have the CancellationToken (not the source), and the UI action itself is the cancellation signal.
        //
        //              The user closing the dialog IS the cancellation event in this context, so we throw directly rather than
        //              waiting for an external token source to signal cancellation.
        //
        //              Note: Because this is an installer, the caller will catch this OperationCanceledException and call
        //              context.CancelAndThrow() to properly handle the cancellation through the job framework. So for consistency,
        //              the token will be correctly signaled as canceled.
        if (!shouldInstall) throw new OperationCanceledException("The user chose to abort the installation");

        deploymentData.CreateLoadoutItems(tree, loadout, loadoutGroup, transaction);
        return new Success();
    }

    private async ValueTask<(bool shouldInstall, DeploymentData data)> GetDeploymentDataAsync(
        string title,
        KeyedBox<RelativePath, LibraryArchiveTree> archiveFiles,
        Loadout.ReadOnly loadout)
    {
        var vm = new AdvancedInstallerWindowViewModel(title, archiveFiles, loadout, showUnsupportedStep: !WasOpenedDirectly);
        await ShowAdvancedInstallerDialog(vm);

        return (vm.AdvancedInstallerVM.ShouldInstall, vm.AdvancedInstallerVM.BodyViewModel.DeploymentData);
    }

    /// <summary>
    /// Creates a modal Dialog window and shows it, then awaits for it to close.
    /// </summary>
    /// <param name="dialogVM">The View Model of the dialog to create.</param>
    private async Task ShowAdvancedInstallerDialog(IAdvancedInstallerWindowViewModel dialogVM)
    {
        var tcs = new TaskCompletionSource();

        OnUi((dialogVM, tcs, _logger), static async tuple =>
        {
            var view = new AdvancedInstallerWindowView
            {
                ViewModel = tuple.dialogVM
            };

            // Get the main window.
            // TODO: This doesn't work with multi-window, this should get the Active window
            if (Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            {
                // Create the modal dialog, parent window is required for modal.
                await view.ShowDialog(desktop.MainWindow);
            }
            else
            {
                // Note: Temporary workaround for when the main window is not found
                tuple._logger.LogError("Failed to find the main window for the Advanced Installer Dialog");
                tuple._logger.LogInformation("Starting Advanced Installer Dialog without a parent window");
                
                // This is not an async method, so we need to subscribe to the close event to make this a task.
                view.Show();
                view.Closed += (_, _) => tuple.tcs.SetResult();
                
                return;
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
