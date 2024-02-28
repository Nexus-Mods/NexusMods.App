using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>, IMainWindowViewModel
{
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly ILoadoutRegistry _registry;
    private readonly IWindowManager _windowManager;

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        ILogger<MainWindowViewModel> logger,
        IOSInformation osInformation,
        IWindowManager windowManager,
        IOverlayController controller,
        IDownloadService downloadService,
        IArchiveInstaller archiveInstaller,
        IMetricsOptInViewModel metricsOptInViewModel,
        IUpdaterViewModel updaterViewModel,
        ILoadoutRegistry registry)
    {
        // NOTE(erri120): can't use DI for VMs that require an active Window because
        // those VMs would be instantiated before this constructor gets called.
        // Use GetRequiredService<TVM> instead.
        _windowManager = windowManager;
        _windowManager.RegisterWindow(this);

        WorkspaceController = new WorkspaceController(
            window: this,
            serviceProvider: serviceProvider
        );

        TopBar = serviceProvider.GetRequiredService<ITopBarViewModel>();
        TopBar.AddPanelDropDownViewModel = new AddPanelDropDownViewModel(WorkspaceController);

        Spine = serviceProvider.GetRequiredService<ISpineViewModel>();
        DevelopmentBuildBanner = serviceProvider.GetRequiredService<IDevelopmentBuildBannerViewModel>();

        _archiveInstaller = archiveInstaller;
        _registry = registry;

        // initialize the diagnostic manager if it hasn't been already
        serviceProvider.GetRequiredService<IDiagnosticManager>();

        // Only show controls in Windows since we can remove the chrome on that platform
        TopBar.ShowWindowControls = osInformation.IsWindows;

        this.WhenActivated(d =>
        {
            // When the user closes the window, we should persist all download state such that it shows
            // accurate values after a reboot.
            // If we ever plan to remove and re-add main window (unlikely), this might need changing to not dispose but only save.
            downloadService.DisposeWith(d);

            downloadService.AnalyzedArchives.Subscribe(tuple =>
            {
                // Because HandleDownloadedAnalyzedArchive is an async task, it begins automatically.
                HandleDownloadedAnalyzedArchive(tuple.task, tuple.downloadId, tuple.modName).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        logger.LogError(t.Exception, "Error while installing downloaded analyzed archive");
                });
            }).DisposeWith(d);

            controller.ApplyNextOverlay.Subscribe(item =>
                {
                    if (item == null)
                    {
                        OverlayContent = null;
                        return;
                    }

                    // This is the main window, if no reference control is specified, show it here.
                    if (item.Value.ViewItem == null)
                        OverlayContent = item.Value.VM;
                    else
                    {
                        // TODO: Determine if we are the right window. For now we do nothing, until that helper is implemented
                        OverlayContent = item.Value.VM;
                    }
                })
                .DisposeWith(d);

            // Only show the updater if the metrics opt-in has been shown before, so we don't spam the user.
            if (!metricsOptInViewModel.MaybeShow())
                updaterViewModel.MaybeShow();

            this.WhenAnyValue(vm => vm.Spine.LeftMenuViewModel)
                .BindToVM(this, vm => vm.LeftMenu)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.IsActive)
                .Where(isActive => isActive)
                .Select(_ => WindowId)
                .BindTo(_windowManager, manager => manager.ActiveWindowId)
                .DisposeWith(d);

            Disposable.Create(this, vm =>
            {
                vm._windowManager.UnregisterWindow(vm);
            }).DisposeWith(d);

            if (!_windowManager.RestoreWindowState(this))
            {
                // NOTE(erri120): select home on startup if we didn't restore the previous state
                Spine.NavigateToHome();
            }
        });
    }

    internal void OnClose()
    {
        // NOTE(erri120): This gets called by the View and can't be inside the disposable
        // of the VM because the MainWindowViewModel gets disposed last, after its contents.
        _windowManager.SaveWindowState(this);
    }

    private async Task HandleDownloadedAnalyzedArchive(IDownloadTask task, DownloadId downloadId, string modName)
    {
        var loadouts = Array.Empty<LoadoutId>();
        if (task is IHaveGameDomain gameDomain)
            loadouts = _registry.AllLoadouts().Where(x => x.Installation.Game.Domain == gameDomain.GameDomain)
                .Select(x => x.LoadoutId).ToArray();

        // Insert code here to invoke loadout picker and get results for final loadouts to install to.
        // ...

        // Install in the background, to avoid blocking UI.
        await Task.Run(async () =>
        {
            if (loadouts.Length > 0)
                await _archiveInstaller.AddMods(loadouts[0], downloadId, modName);
            else
                await _archiveInstaller.AddMods(_registry.AllLoadouts().First().LoadoutId, downloadId, modName);
        });
    }

    public WindowId WindowId { get; } = WindowId.NewId();

    /// <inheritdoc/>
    [Reactive] public bool IsActive { get; set; }

    /// <inheritdoc/>
    public IWorkspaceController WorkspaceController { get; }

    [Reactive] public ISpineViewModel Spine { get; set; }

    [Reactive] public ILeftMenuViewModel? LeftMenu { get; set; }

    [Reactive]
    public ITopBarViewModel TopBar { get; set; }

    [Reactive]
    public IDevelopmentBuildBannerViewModel DevelopmentBuildBanner { get; set; }

    [Reactive]
    public IOverlayViewModel? OverlayContent { get; set; }
}
