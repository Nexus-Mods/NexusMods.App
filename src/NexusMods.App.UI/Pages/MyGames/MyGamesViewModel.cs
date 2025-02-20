using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.MiniGameWidget;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;
using OneOf.Types;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData.Aggregation;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.CrossPlatform.Process;
using NexusMods.Telemetry;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    private const string TrelloPublicRoadmapUrl = "https://trello.com/b/gPzMuIr3/nexus-mods-app-roadmap";

    private readonly IWindowManager _windowManager;
    private readonly IJobMonitor _jobMonitor;

    private ReadOnlyObservableCollection<IMiniGameWidgetViewModel> _supportedGames = new([]);
    private ReadOnlyObservableCollection<IGameWidgetViewModel> _installedGames = new([]);

    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenRoadmapCommand { get; }
    public ReadOnlyObservableCollection<IGameWidgetViewModel> InstalledGames => _installedGames;
    public ReadOnlyObservableCollection<IMiniGameWidgetViewModel> SupportedGames => _supportedGames;

    public MyGamesViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        IConnection conn,
        ILogger<MyGamesViewModel> logger,
        IOverlayController overlayController,
        IOSInterop osInterop,
        ISynchronizerService syncService,
        IGameRegistry gameRegistry) : base(windowManager)
    {
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();

        TabTitle = Language.MyGames;
        TabIcon = IconValues.GamepadOutline;

        var provider = serviceProvider;
        _windowManager = windowManager;

        var workspaceController = windowManager.ActiveWorkspaceController;

        GiveFeedbackCommand = ReactiveCommand.Create(() =>
            {
                var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
                alphaWarningViewModel.WorkspaceController = workspaceController;

                overlayController.Enqueue(alphaWarningViewModel);
            }
        );

        OpenRoadmapCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var uri = new Uri(TrelloPublicRoadmapUrl);
                await osInterop.OpenUrl(uri);
            }
        );

        this.WhenActivated(d =>
            {
                gameRegistry.InstalledGames
                    .ToObservableChangeSet()
                    .Transform(installation =>
                        {
                            var vm = provider.GetRequiredService<IGameWidgetViewModel>();
                            vm.Installation = installation;

                            vm.AddGameCommand = ReactiveCommand.CreateFromTask(async () =>
                            {
                                if (GetJobRunningForGameInstallation(installation).IsT1) return;

                                vm.State = GameWidgetState.AddingGame;
                                await Task.Run(async () => await ManageGame(installation));
                                vm.State = GameWidgetState.ManagedGame;
                                
                                Tracking.AddEvent(Events.Game.AddGame, new EventMetadata(name: installation.Game.Name));
                                
                                NavigateToFirstLoadoutLibrary(conn, installation);
                            });

                            vm.RemoveAllLoadoutsCommand = ReactiveCommand.CreateFromTask(async () =>
                            {
                                if (GetJobRunningForGameInstallation(installation).IsT2) return;

                                vm.State = GameWidgetState.RemovingGame;
                                await Task.Run(async () => await RemoveAllLoadouts(installation));
                                vm.State = GameWidgetState.DetectedGame;

                                Tracking.AddEvent(Events.Game.RemoveGame, new EventMetadata(name: installation.Game.Name));
                            });

                            vm.ViewGameCommand = ReactiveCommand.Create(() =>
                            {
                                NavigateToFirstLoadoutLibrary(conn, installation);
                                Tracking.AddEvent(Events.Game.ViewGame, new EventMetadata(name: installation.Game.Name));
                            });

                            vm.IsManagedObservable = Loadout.ObserveAll(conn)
                                .Filter(l => l.IsVisible() && l.InstallationInstance.GameMetadataId == installation.GameMetadataId)
                                .Count()
                                .Select(c => c > 0);

                            var job = GetJobRunningForGameInstallation(installation);

                            // fixes when the page loads and a job is still running
                            vm.State = job.Value switch
                            {
                                CreateLoadoutJob _ => GameWidgetState.AddingGame,
                                UnmanageGameJob _ => GameWidgetState.RemovingGame,
                                _ => GameWidgetState.DetectedGame,
                            };

                            return vm;
                        }
                    )
                    .OnUI()
                    .Bind(out _installedGames)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(d);

                // NOTE(insomnious): The weird cast is so that we don't get a circular reference with Abstractions.Games when
                // referencing Abstractions.GameLocators directly 
                var supportedGamesAsIGame = gameRegistry.SupportedGames.Cast<IGame>();

                var miniGameWidgetViewModels = supportedGamesAsIGame
                    .Select(game =>
                        {
                            var vm = provider.GetRequiredService<IMiniGameWidgetViewModel>();
                            vm.Game = game;
                            vm.Name = game.Name;
                            // is this supported game installed?
                            vm.IsFound = _installedGames.Any(install => install.Installation.GetGame().GameId == game.GameId);
                            vm.GameInstallations = _installedGames
                                .Where(install => install.Installation.GetGame().GameId == game.GameId)
                                .Select(install => install.Installation)
                                .ToArray();
                            return vm;
                        }
                    )
                    .OrderByDescending(vm => vm.IsFound)
                    .ToList();

                _supportedGames = new ReadOnlyObservableCollection<IMiniGameWidgetViewModel>(new ObservableCollection<IMiniGameWidgetViewModel>(miniGameWidgetViewModels));
            }
        );
    }

    private OneOf<None, CreateLoadoutJob, UnmanageGameJob> GetJobRunningForGameInstallation(GameInstallation installation)
    {
        foreach (var job in _jobMonitor.Jobs)
        {
            if (job.Status != JobStatus.Running) continue;

            if (job.Definition is CreateLoadoutJob createLoadoutJob && createLoadoutJob.Installation.Equals(installation)) return createLoadoutJob;
            if (job.Definition is UnmanageGameJob unmanageGameJob && unmanageGameJob.Installation.Equals(installation)) return unmanageGameJob;
        }

        return OneOf<None, CreateLoadoutJob, UnmanageGameJob>.FromT0(new None());
    }

    private async Task RemoveAllLoadouts(GameInstallation installation)
    {
        await installation.GetGame().Synchronizer.UnManage(installation);
    }

    private async Task ManageGame(GameInstallation installation)
    {
        await installation.GetGame().Synchronizer.CreateLoadout(installation);
    }
    
    private Optional<LoadoutId> GetFirstLoadoutId(IConnection conn, GameInstallation installation)
    {
        var db = conn.Db;

        var loadout = Loadout.All(db).FirstOrOptional(loadout => 
            loadout.IsVisible() && loadout.InstallationInstance.Equals(installation));
        
        return loadout.HasValue ? loadout.Value.LoadoutId : Optional<LoadoutId>.None;
    }
    
    private void NavigateToFirstLoadoutLibrary(IConnection conn, GameInstallation installation)
    {
        var fistLoadout = GetFirstLoadoutId(conn, installation);
        if (!fistLoadout.HasValue) return;
        var loadoutId = fistLoadout.Value;
        Dispatcher.UIThread.Invoke(() =>
            {
                var workspaceController = _windowManager.ActiveWorkspaceController;
                
                workspaceController.ChangeOrCreateWorkspaceByContext(
                    context => context.LoadoutId == loadoutId,
                    () => new PageData
                    {
                        FactoryId = LibraryPageFactory.StaticId,
                        Context = new LibraryPageContext()
                        {
                            LoadoutId = loadoutId,
                        },
                    },
                    () => new LoadoutContext
                    {
                        LoadoutId = loadoutId,
                    }
                );
            }
        );
    }
}
