using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.EventBus;
using NexusMods.Abstractions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CLI;
using NexusMods.CrossPlatform;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Disposable = System.Reactive.Disposables.Disposable;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>, IMainWindowViewModel
{
    private readonly IWindowManager _windowManager;

    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, bool> BringWindowToFront { get; }
    public ReactiveUI.ReactiveCommand<IStorageProvider, System.Reactive.Unit> RegisterStorageProvider { get; }

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager,
        IOverlayController overlayController,
        ILoginManager loginManager,
        IEventBus eventBus,
        ISettingsManager settingsManager)
    {
        var avaloniaInterop = serviceProvider.GetRequiredService<IAvaloniaInterop>();

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

        BringWindowToFront = ReactiveCommand.Create(() => settingsManager.Get<BehaviorSettings>().BringWindowToFront);
        RegisterStorageProvider = ReactiveCommand.Create<IStorageProvider>(storageProvider => avaloniaInterop.RegisterStorageProvider(storageProvider));

        this.WhenActivated(d =>
        {
            ConnectErrors(serviceProvider).DisposeWith(d);

            var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
            alphaWarningViewModel.WorkspaceController = WorkspaceController;
            alphaWarningViewModel.Controller = overlayController;
            alphaWarningViewModel.MaybeShow();

            var metricsOptInViewModel = serviceProvider.GetRequiredService<IMetricsOptInViewModel>();
            metricsOptInViewModel.Controller = overlayController;
            metricsOptInViewModel.MaybeShow();

            R3.Observable
                .Return(UpdateChecker.ShouldCheckForUpdate())
                .Where(shouldCheck => shouldCheck)
                .ObserveOnThreadPool()
                .SelectAwait(
                    selector: (_, cancellationToken) => UpdaterViewModel.CreateIfNeeded(serviceProvider, cancellationToken),
                    configureAwait: false
                )
                .ObserveOnUIThreadDispatcher()
                .WhereNotNull()
                .Subscribe(overlayController, static (overlay, overlayController) =>
                {
                    overlay.Controller = overlayController;
                    overlayController.Enqueue(overlay);
                })
                .AddTo(d);

            loginManager.IsLoggedInObservable
                .DistinctUntilChanged()
                .Where(isSignedIn => isSignedIn)
                .Select(_ => System.Reactive.Unit.Default)
                .InvokeReactiveCommand(BringWindowToFront)
                .DisposeWith(d);
            
            var loginMessageVM = serviceProvider.GetRequiredService<ILoginMessageBoxViewModel>();
            loginMessageVM.Controller = overlayController;
            loginMessageVM.MaybeShow();

            this.WhenAnyValue(vm => vm.Spine.LeftMenuViewModel)
                .BindToVM(this, vm => vm.LeftMenu)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.IsActive)
                .Where(isActive => isActive)
                .Select(_ => this)
                .BindTo(_windowManager, manager => manager.ActiveWindow)
                .DisposeWith(d);
            
            overlayController.WhenAnyValue(oc => oc.CurrentOverlay)
                .BindTo(this, vm => vm.CurrentOverlay)
                .DisposeWith(d);

            eventBus
                .ObserveMessages<CliMessages.AddedCollection>()
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (message, self) =>
                {
                    var workspaceController = self.WorkspaceController;
                    if (workspaceController.ActiveWorkspace.Context is not LoadoutContext loadoutContext) return;

                    var pageData = new PageData
                    {
                        FactoryId = CollectionDownloadPageFactory.StaticId,
                        Context = new CollectionDownloadPageContext
                        {
                            TargetLoadout = loadoutContext.LoadoutId,
                            CollectionRevisionMetadataId = message.Revision,
                        },
                    };

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, NavigationInput.Default);
                    workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);

                    using var _ = self.BringWindowToFront.Execute(System.Reactive.Unit.Default).Subscribe();
                })
                .DisposeWith(d);

            eventBus
                .ObserveMessages<CliMessages.AddedDownload>()
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (message, self) =>
                {
                    using var _ = self.BringWindowToFront.Execute(System.Reactive.Unit.Default).Subscribe();
                })
                .DisposeWith(d);

            R3.Disposable.Create(this, vm =>
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

    private IDisposable ConnectErrors(IServiceProvider provider)
    {
        var settings = provider.GetRequiredService<ISettingsManager>().Get<LoggingSettings>();
        if (!settings.ShowExceptions) return Disposable.Empty;

        var source = provider.GetService<IObservableExceptionSource>();
        if (source is null) return Disposable.Empty;

        return source.Exceptions
            .Subscribe(msg =>
                {
                    var title = "Unhandled Exception";
                    var description = msg.Message;
                    string? details = null;
                    if (msg.Exception != null)
                    {
                        details = $"""
                                  ```
                                  {msg.Exception}
                                  ``` 
                                  """;
                    }

                    Task.Run(() => MessageBoxOkViewModel.Show(provider, title, description, details));
                }
            );
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
