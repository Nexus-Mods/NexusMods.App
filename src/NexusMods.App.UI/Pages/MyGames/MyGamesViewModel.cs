using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Threading;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly IServiceProvider _provider;
    private readonly IWindowManager _windowManager;
    private readonly ILogger<MyGamesViewModel> _logger;
    private readonly IApplyService _applyService;
    private ReadOnlyObservableCollection<IGameWidgetViewModel> _managedGames = new([]);
    private ReadOnlyObservableCollection<IGameWidgetViewModel> _detectedGames = new([]);

    public ReadOnlyObservableCollection<IGameWidgetViewModel> ManagedGames => _managedGames;
    public ReadOnlyObservableCollection<IGameWidgetViewModel> DetectedGames => _detectedGames;

    public MyGamesViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        ILoadoutRegistry loadoutRegistry,
        ILogger<MyGamesViewModel> logger,
        IApplyService applyService,
        IEnumerable<IGame> games) : base(windowManager)
    {
        TabTitle = Language.MyGames;
		TabIcon = IconValues.JoystickGameFilled;
        
        _provider = serviceProvider;
        _loadoutRegistry = loadoutRegistry;
        _applyService = applyService;
        _windowManager = windowManager;
        _logger = logger;

        var gamesList = games.ToList();
        var installations = gamesList
            .SelectMany(game => game.Installations)
            .ToObservableCollection()
            .AsObservableChangeSet();

        this.WhenActivated(d =>
            {
                var managedInstallations = _loadoutRegistry.LoadoutRootChanges
                    .Transform(loadoutId => (loadoutId, loadout: loadoutRegistry.Get(loadoutId)))
                    .Filter(tuple => tuple.loadout != null)
                    .DistinctValues(tuple => tuple.loadout!.Installation);

                // Managed games widgets
                managedInstallations
                    .OnUI()
                    .Transform(install =>
                    {
                        var vm = _provider.GetRequiredService<IGameWidgetViewModel>();
                        vm.Installation = install;
                        vm.AddGameCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            vm.State = GameWidgetState.AddingGame;
                            await Task.Run(async () => await ManageGame(install));
                            vm.State = GameWidgetState.ManagedGame;
                        });
                        vm.RemoveAllLoadoutsCommand = ReactiveCommand.CreateFromTask(async () => 
                        {
                            vm.State = GameWidgetState.RemovingGame;
                            await Task.Run(async () => await RemoveAllLoadouts(install));
                            vm.State = GameWidgetState.ManagedGame;
                        });

                        vm.ViewGameCommand = ReactiveCommand.Create(
                            () => { NavigateToLoadout(install); }
                        );

                        vm.State = GameWidgetState.ManagedGame;
                        return vm;
                    })
                    .Bind(out _managedGames)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(d);

                // Detected games widgets, except already managed games
                installations
                    .Except(managedInstallations)
                    .OnUI()
                    .Transform(install =>
                    {
                        var vm = _provider.GetRequiredService<IGameWidgetViewModel>();
                        vm.Installation = install;
                        vm.AddGameCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            vm.State = GameWidgetState.AddingGame;
                            await Task.Run(async () => await ManageGame(install));
                            vm.State = GameWidgetState.ManagedGame;
                        });
                        vm.RemoveAllLoadoutsCommand = ReactiveCommand.CreateFromTask(async () => 
                        {
                            vm.State = GameWidgetState.RemovingGame;
                            await Task.Run(async () => await RemoveAllLoadouts(install));
                            vm.State = GameWidgetState.ManagedGame;
                        });

                        vm.State = GameWidgetState.DetectedGame;
                        return vm;
                    })
                    .Bind(out _detectedGames)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(d);
            }
        );
    }

    private async Task RemoveAllLoadouts(GameInstallation install)
    {
        var synchronizer = install.GetGame().Synchronizer;
        await synchronizer.UnManage(install);
    }

    private async Task ManageGame(GameInstallation installation)
    {
        var name = _loadoutRegistry.SuggestName(installation);
        var marker = await _loadoutRegistry.Manage(installation, name);

        var loadoutId = marker.Id;

        Dispatcher.UIThread.Invoke(() =>
            {
                if (!_windowManager.TryGetActiveWindow(out var window)) return;
                var workspaceController = window.WorkspaceController;

                workspaceController.ChangeOrCreateWorkspaceByContext(
                    context => context.LoadoutId == loadoutId,
                    () => new PageData
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext
                        {
                            LoadoutId = loadoutId
                        }
                    },
                    () => new LoadoutContext
                    {
                        LoadoutId = loadoutId
                    }
                );
            }
        );
    }

    private void NavigateToLoadout(GameInstallation installation)
    {
        var revId = _applyService.GetLastAppliedLoadout(installation);
        if (revId is null)
        {
            _logger.LogError("Unable to find active loadout for  {GameName} : {InstallPath}",
                installation.Game.Name,
                installation.LocationsRegister[LocationId.Game]
            );
            return;
        }

        var loadout = _loadoutRegistry.GetLoadout(revId);
        if (loadout is null)
        {
            _logger.LogError("Unable to find loadout for revision {RevId}", revId);
            return;
        }

        var loadoutId = loadout.LoadoutId;

        Dispatcher.UIThread.Invoke(() =>
            {
                if (!_windowManager.TryGetActiveWindow(out var window)) return;
                var workspaceController = window.WorkspaceController;

                workspaceController.ChangeOrCreateWorkspaceByContext(
                    context => context.LoadoutId == loadoutId,
                    () => new PageData
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext
                        {
                            LoadoutId = loadoutId
                        }
                    },
                    () => new LoadoutContext
                    {
                        LoadoutId = loadoutId
                    }
                );
            }
        );
    }
}
