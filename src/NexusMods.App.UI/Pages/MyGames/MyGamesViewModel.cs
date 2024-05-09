using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
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
        IConnection conn,
        ILogger<MyGamesViewModel> logger,
        IApplyService applyService,
        IGameRegistry gameRegistry,
        IRepository<Loadout.Model> loadoutRepository) : base(windowManager)
    {
        TabTitle = Language.MyGames;
		TabIcon = IconValues.JoystickGameFilled;
        
        var provider = serviceProvider;
        _applyService = applyService;
        _windowManager = windowManager;
        _logger = logger;
        
        this.WhenActivated(d =>
            {
                var managedInstallations = loadoutRepository.Observable
                    .ToObservableChangeSet()
                    .DistinctValues(model => model.Installation);

                // Managed games widgets
                managedInstallations
                    .OnUI()
                    .Transform(install =>
                    {
                        var vm = provider.GetRequiredService<IGameWidgetViewModel>();
                        vm.Installation = install;
                        vm.AddGameCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            vm.State = GameWidgetState.AddingGame;
                            await Task.Run(async () => await ManageGame(install));
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
                gameRegistry.InstalledGames
                    .ToObservableChangeSet()
                    .Except(managedInstallations)
                    .OnUI()
                    .Transform(install =>
                    {
                        var vm = provider.GetRequiredService<IGameWidgetViewModel>();
                        vm.Installation = install;
                        vm.AddGameCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            vm.State = GameWidgetState.AddingGame;
                            await Task.Run(async () => await ManageGame(install));
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

    private async Task ManageGame(GameInstallation installation)
    {
        
        var install = await ((IGame)installation.Game).Synchronizer.Manage(installation);

        Dispatcher.UIThread.Invoke(() =>
            {
                if (!_windowManager.TryGetActiveWindow(out var window)) return;
                var workspaceController = window.WorkspaceController;

                workspaceController.ChangeOrCreateWorkspaceByContext(
                    context => context.LoadoutId == install.LoadoutId,
                    () => new PageData
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext
                        {
                            LoadoutId = install.LoadoutId,
                        },
                    },
                    () => new LoadoutContext
                    {
                        LoadoutId = install.LoadoutId,
                    }
                );
            }
        );
    }

    private void NavigateToLoadout(GameInstallation installation)
    {
        
        var loadout = _applyService.GetLastAppliedLoadout(installation);
        if (loadout is null)
        {
            _logger.LogError("Unable to find active loadout for  {GameName} : {InstallPath}",
                installation.Game.Name,
                installation.LocationsRegister[LocationId.Game]
            );
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
