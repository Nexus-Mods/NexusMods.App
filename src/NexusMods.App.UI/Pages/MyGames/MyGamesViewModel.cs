using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
using NexusMods.MnemonicDB.Abstractions.Query;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    private readonly IWindowManager _windowManager;
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
        IGameRegistry gameRegistry) : base(windowManager)
    {
        TabTitle = Language.MyGames;
		TabIcon = IconValues.Game;
        
        var provider = serviceProvider;
        _windowManager = windowManager;
        
        this.WhenActivated(d =>
            {
                // Managed games widgets
                Loadout.ObserveAll(conn)
                    .Filter(l => l.IsVisible())
                    .OnUI()
                    .Transform(loadout =>
                    {
                        var vm = provider.GetRequiredService<IGameWidgetViewModel>();
                        vm.Installation = loadout.InstallationInstance;
                        vm.AddGameCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            vm.State = GameWidgetState.AddingGame;
                            await Task.Run(async () => await ManageGame(loadout.InstallationInstance));
                            vm.State = GameWidgetState.ManagedGame;
                        });
                        vm.RemoveAllLoadoutsCommand = ReactiveCommand.CreateFromTask(async () => 
                        {
                            vm.State = GameWidgetState.RemovingGame;
                            await Task.Run(async () => await RemoveAllLoadouts(loadout.InstallationInstance));
                            vm.State = GameWidgetState.ManagedGame;
                        });

                        vm.ViewGameCommand = ReactiveCommand.Create(
                            () => { NavigateToLoadout(conn, loadout); }
                        );

                        vm.State = GameWidgetState.ManagedGame;
                        return vm;
                    })
                    .Bind(out _managedGames)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(d);

                // For the games that are detected, we only want to show those that are not managed, we'll bind directly
                // to the collection here so we don't need any temporary collections or observables
                gameRegistry.InstalledGames
                    .ToObservableChangeSet()
                    .Except(_managedGames.ToObservableChangeSet().Transform(s => s.Installation))
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
        var install = await ((IGame)installation.Game).Synchronizer.CreateLoadout(installation);
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

    private void NavigateToLoadout(IConnection conn, Loadout.ReadOnly loadout)
    {
        // We can't navigate to an invisible loadout, make sure we pick a visible one.
        var db = conn.Db;
        if (!loadout.IsVisible())
        {
            // Note(sewer) | If we're here, last loadout was most likely a LoadoutKind.VanillaState
            loadout = Loadout.All(db).First(x => x.IsVisible());
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
