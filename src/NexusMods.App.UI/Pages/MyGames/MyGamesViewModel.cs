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
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Extensions.BCL;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;
using OneOf.Types;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    private readonly IWindowManager _windowManager;
    private readonly IJobMonitor _jobMonitor;

    private ReadOnlyObservableCollection<IGameWidgetViewModel> _managedGames = new([]);
    private ReadOnlyObservableCollection<IGameWidgetViewModel> _detectedGames = new([]);

    public ReadOnlyObservableCollection<IGameWidgetViewModel> ManagedGames => _managedGames;
    public ReadOnlyObservableCollection<IGameWidgetViewModel> DetectedGames => _detectedGames;

    public MyGamesViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        IConnection conn,
        ILogger<MyGamesViewModel> logger,
        ISynchronizerService syncService,
        IGameRegistry gameRegistry) : base(windowManager)
    {
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();

        TabTitle = Language.MyGames;
		TabIcon = IconValues.Game;
        
        var provider = serviceProvider;
        _windowManager = windowManager;
        
        this.WhenActivated(d =>
        {
            // Managed games widgets
            Loadout.ObserveAll(conn)
                .Filter(l => l.IsVisible())
                .RemoveKey()
                .GroupOn(loadout => loadout.InstallationInstance.LocationsRegister[LocationId.Game])
                .Transform(group=> group.List.Items.First())
                .OnUI()
                .Transform(loadout =>
                {
                    var vm = provider.GetRequiredService<IGameWidgetViewModel>();
                    vm.Installation = loadout.InstallationInstance;

                    vm.RemoveAllLoadoutsCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        if (GetJobRunningForGameInstallation(loadout.InstallationInstance).IsT2) return;

                        vm.State = GameWidgetState.RemovingGame;
                        await Task.Run(async () => await RemoveAllLoadouts(loadout.InstallationInstance));
                        vm.State = GameWidgetState.ManagedGame;
                    });

                    vm.ViewGameCommand = ReactiveCommand.Create(() => { NavigateToLoadout(conn, loadout); });

                    vm.State = ToState(GetJobRunningForGameInstallation(loadout.InstallationInstance), GameWidgetState.ManagedGame);
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
                        if (GetJobRunningForGameInstallation(install).IsT1) return;

                        vm.State = GameWidgetState.AddingGame;
                        await Task.Run(async () => await ManageGame(install));
                        vm.State = GameWidgetState.ManagedGame;
                    });

                    vm.State = ToState(GetJobRunningForGameInstallation(install), GameWidgetState.DetectedGame);
                    return vm;
                })
                .Bind(out _detectedGames)
                .SubscribeWithErrorLogging()
                .DisposeWith(d);
        });
    }

    private static GameWidgetState ToState(OneOf<None, CreateLoadoutJob, UnmanageGameJob> res, GameWidgetState initial)
    {
        return res.Match(
            f0: _ => initial,
            f1: _ => GameWidgetState.AddingGame,
            f2: _ => GameWidgetState.RemovingGame
        );
    }

    private OneOf<None, CreateLoadoutJob, UnmanageGameJob> GetJobRunningForGameInstallation(GameInstallation installation)
    {
        foreach (var job in _jobMonitor.Jobs)
        {
            if (job.Status != JobStatus.Running) continue;

            if (job is CreateLoadoutJob createLoadoutJob && createLoadoutJob.Installation.Equals(installation)) return createLoadoutJob;
            if (job is UnmanageGameJob unmanageGameJob && unmanageGameJob.Installation.Equals(installation)) return unmanageGameJob;
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
                var workspaceController = _windowManager.ActiveWorkspaceController;

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
