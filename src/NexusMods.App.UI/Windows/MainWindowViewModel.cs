using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>, IMainWindowViewModel
{
    private readonly IWindowManager _windowManager;
    
    public ReactiveCommand<Unit, Unit> BringWindowToFront { get; } = ReactiveCommand.Create(() => { });

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        IOSInformation osInformation,
        IWindowManager windowManager,
        IOverlayController overlayController,
        ILoginManager loginManager)
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

        // Only show controls in Windows since we can remove the chrome on that platform
        TopBar.ShowWindowControls = osInformation.IsWindows;
        
        this.WhenActivated(d =>
        {
            var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
            alphaWarningViewModel.WorkspaceController = WorkspaceController;
            alphaWarningViewModel.Controller = overlayController;
            alphaWarningViewModel.MaybeShow();

            var metricsOptInViewModel = serviceProvider.GetRequiredService<IMetricsOptInViewModel>();
            metricsOptInViewModel.Controller = overlayController;

            // Only show the updater if the metrics opt-in has been shown before, so we don't spam the user.
            if (!metricsOptInViewModel.MaybeShow())
            {
                var updaterViewModel = serviceProvider.GetRequiredService<IUpdaterViewModel>();
                updaterViewModel.MaybeShow();
            }
            
            loginManager.IsLoggedInObservable
                .Where(isSignedIn => isSignedIn)
                .InvokeCommand(BringWindowToFront)
                .DisposeWith(d);
            
            var loginMessageVM = serviceProvider.GetRequiredService<ILoginMessageBoxViewModel>();
            loginMessageVM.Controller = overlayController;
            loginMessageVM.MaybeShow();

            this.WhenAnyValue(vm => vm.Spine.LeftMenuViewModel)
                .BindToVM(this, vm => vm.LeftMenu)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.IsActive)
                .Where(isActive => isActive)
                .Select(_ => WindowId)
                .BindTo(_windowManager, manager => manager.ActiveWindowId)
                .DisposeWith(d);
            
            overlayController.WhenAnyValue(oc => oc.CurrentOverlay)
                .BindTo(this, vm => vm.CurrentOverlay)
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
    
    public WindowId WindowId { get; } = WindowId.NewId();

    /// <inheritdoc/>
    [Reactive] public bool IsActive { get; set; }
    
    [Reactive]
    public IOverlayViewModel? CurrentOverlay { get; set; }

    /// <inheritdoc/>
    public IWorkspaceController WorkspaceController { get; }


    [Reactive] public ISpineViewModel Spine { get; set; }

    [Reactive] public ILeftMenuViewModel? LeftMenu { get; set; }

    [Reactive]
    public ITopBarViewModel TopBar { get; set; }

    [Reactive]
    public IDevelopmentBuildBannerViewModel DevelopmentBuildBanner { get; set; }
}
