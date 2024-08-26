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
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
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
                        if (IsJobRunningForGameInstallation(loadout.InstallationInstance)) return;

                        vm.State = GameWidgetState.RemovingGame;
                        await Task.Run(async () => await RemoveAllLoadouts(loadout.InstallationInstance));
                        vm.State = GameWidgetState.ManagedGame;
                    });

                    vm.ViewGameCommand = ReactiveCommand.Create(() => { NavigateToLoadout(conn, loadout); });

                    vm.State = IsJobRunningForGameInstallation(loadout.InstallationInstance) ? GameWidgetState.RemovingGame : GameWidgetState.ManagedGame;
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
                        if (IsJobRunningForGameInstallation(install)) return;

                        vm.State = GameWidgetState.AddingGame;
                        await Task.Run(async () => await ManageGame(install));
                        vm.State = GameWidgetState.ManagedGame;
                    });

                    vm.State = IsJobRunningForGameInstallation(install) ? GameWidgetState.AddingGame : GameWidgetState.DetectedGame;
                    return vm;
                })
                .Bind(out _detectedGames)
                .SubscribeWithErrorLogging()
                .DisposeWith(d);
        });
    }

    private bool IsJobRunningForGameInstallation(GameInstallation installation)
    {
        return _jobMonitor.Jobs
            .Any(job =>
            {
                if (job.Status != JobStatus.Running) return false;

                if (job is CreateLoadoutJob createLoadoutJob)
                {
                    return createLoadoutJob.Installation.Equals(installation);
                }

                if (job is UnmanageGameJob unmanageGameJob)
                {
                    return unmanageGameJob.Installation.Equals(installation);
                }

                return false;
            });
    }

    private async Task RemoveAllLoadouts(GameInstallation installation)
    {
        IsJobRunningForGameInstallation(installation);
        await installation.GetGame().Synchronizer.UnManage(installation);
    }

    private async Task ManageGame(GameInstallation installation)
    {
        IsJobRunningForGameInstallation(installation);
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
