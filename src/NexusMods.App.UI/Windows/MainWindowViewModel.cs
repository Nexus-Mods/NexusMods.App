using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Platform.Storage;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.EventBus;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Logging;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CLI;
using NexusMods.CrossPlatform;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Disposable = System.Reactive.Disposables.Disposable;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>, IMainWindowViewModel
{
    private readonly IWindowManager _windowManager;
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;

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
        _serviceProvider = serviceProvider;
        var avaloniaInterop = serviceProvider.GetRequiredService<IAvaloniaInterop>();
        _connection = serviceProvider.GetRequiredService<IConnection>();

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

            var welcomeOverlayViewModel = WelcomeOverlayViewModel.CreateIfNeeded(serviceProvider);
            if (welcomeOverlayViewModel is not null) overlayController.Enqueue(welcomeOverlayViewModel);

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

                    var tuple = self.GetWorkspaceIdForGame(workspaceController, message.Revision.Collection.GameId);
                    if (!tuple.HasValue) return;

                    var (loadoutId, workspaceId) = tuple.Value;

                    var pageData = new PageData
                    {
                        FactoryId = CollectionDownloadPageFactory.StaticId,
                        Context = new CollectionDownloadPageContext
                        {
                            TargetLoadout = loadoutId,
                            CollectionRevisionMetadataId = message.Revision,
                        },
                    };

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, NavigationInput.Default);
                    workspaceController.OpenPage(workspaceId, pageData, behavior);

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

    private Optional<(LoadoutId, WorkspaceId)> GetWorkspaceIdForGame(IWorkspaceController workspaceController, GameId gameId)
    {
        if (workspaceController.ActiveWorkspace.Context is LoadoutContext existingLoadoutContext && IsCorrectLoadoutForGame(existingLoadoutContext.LoadoutId, gameId))
            return (existingLoadoutContext.LoadoutId, workspaceController.ActiveWorkspaceId);

        var loadoutId = GetActiveLoadoutForGame(gameId);
        if (!loadoutId.HasValue) return Optional<(LoadoutId, WorkspaceId)>.None;

        var workspaceViewModel = workspaceController.ChangeOrCreateWorkspaceByContext(
            predicate: loadoutContext => IsCorrectLoadoutForGame(loadoutContext.LoadoutId, gameId),
            getPageData: () => Optional<PageData>.None,
            getWorkspaceContext: () => new LoadoutContext
            {
                LoadoutId = loadoutId.Value,
            }
        );

        return (loadoutId.Value, workspaceViewModel.Id);
    }

    private bool IsCorrectLoadoutForGame(LoadoutId loadoutId, GameId gameId)
    {
        var loadout = Loadout.Load(_connection.Db, loadoutId);
        return loadout.IsValid() && loadout.InstallationInstance.Game.GameId == gameId;
    }

    private Optional<LoadoutId> GetActiveLoadoutForGame(GameId gameId)
    {
        var gameRegistry = _serviceProvider.GetRequiredService<IGameRegistry>();
        if (!gameRegistry.InstalledGames.TryGetFirst(x => x.Game.GameId == gameId, out var gameInstallation)) return Optional<LoadoutId>.None;

        if (gameInstallation.Game is not IGame game) return Optional<LoadoutId>.None;
        return game.Synchronizer.GetCurrentlyActiveLoadout(gameInstallation);
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
