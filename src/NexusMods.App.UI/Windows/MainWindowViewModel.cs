using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>
{
    private readonly IOverlayController _overlayController;
    private readonly IArchiveInstaller _archiveInstaller;
    private LoadoutRegistry _registry;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IOSInformation osInformation,
        ISpineViewModel spineViewModel,
        ITopBarViewModel topBarViewModel,
        IDevelopmentBuildBannerViewModel developmentBuildBannerViewModel,
        IOverlayController controller,
        IDownloadService downloadService,
        IArchiveInstaller archiveInstaller,
        IMetricsOptInViewModel metricsOptInViewModel,
        LoadoutRegistry registry)
    {
        TopBar = topBarViewModel;
        Spine = spineViewModel;
        DevelopmentBuildBanner = developmentBuildBannerViewModel;
        _overlayController = controller;
        _archiveInstaller = archiveInstaller;
        _registry = registry;

        // Only show controls in Windows since we can remove the chrome on that platform
        TopBar.ShowWindowControls = osInformation.IsWindows;

        this.WhenActivated(d =>
        {
            Spine.Actions
                .SubscribeWithErrorLogging(logger, HandleSpineAction)
                .DisposeWith(d);

            // When the user closes the window, we should persist all download state such that it shows
            // accurate values after a reboot.
            // If we ever plan to remove and re-add main window (unlikely), this might need changing to not dispose but only save.
            downloadService.DisposeWith(d);

            downloadService.AnalyzedArchives.Subscribe(tuple =>
            {
                // Because HandleDownloadedAnalyzedArchive is an async task, it begins automatically.
                HandleDownloadedAnalyzedArchive(tuple.task, tuple.analyzedHash, tuple.modName).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        logger.LogError(t.Exception, "Error while installing downloaded analyzed archive");
                });
            }).DisposeWith(d);

            _overlayController.ApplyNextOverlay.Subscribe(item =>
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

            this.WhenAnyValue(vm => vm.Spine.LeftMenu)
                .Select(left =>
                {
                    logger.LogDebug("Spine changed left menu to {LeftMenu}", left);
                    return left;
                })
                .BindTo(this, vm => vm.LeftMenu)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.LeftMenu.RightContent)
                .Select(right =>
                {
                    logger.LogDebug(
                        "Left menu changed right content to {RightContent}",
                        right);
                    return right;
                }).BindTo(this, vm => vm.RightContent);

            metricsOptInViewModel.MaybeShow();
        });
    }


    private async Task HandleDownloadedAnalyzedArchive(IDownloadTask task, Hash analyzedHash, string modName)
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
                await _archiveInstaller.AddMods(loadouts[0], analyzedHash, modName);
            else
                await _archiveInstaller.AddMods(_registry.AllLoadouts().First().LoadoutId, analyzedHash, modName);
        });
    }

    private void HandleSpineAction(SpineButtonAction action)
    {
        Spine.Activations.OnNext(action);
    }

    [Reactive]
    public ISpineViewModel Spine { get; set; }

    [Reactive]
    public IViewModelInterface RightContent { get; set; } =
        Initializers.IRightContent;

    [Reactive]
    public ILeftMenuViewModel LeftMenu { get; set; } =
        Initializers.ILeftMenuViewModel;

    [Reactive]
    public ITopBarViewModel TopBar { get; set; }

    [Reactive]
    public IDevelopmentBuildBannerViewModel DevelopmentBuildBanner { get; set; }

    [Reactive]
    public IOverlayViewModel? OverlayContent { get; set; }
}
