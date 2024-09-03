using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DurableJobs;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
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
    private readonly IJobManager _jobManager;

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
        _jobManager = serviceProvider.GetRequiredService<IJobManager>();

        TabTitle = Language.MyGames;
		TabIcon = IconValues.Game;
        
        var provider = serviceProvider;
        _windowManager = windowManager;
        
        this.WhenActivated(d =>
        {
            // Managed games widgets
            Loadout.ObserveAll(conn)
                .Filter(l => l.IsVisible())
                .DistinctValues(loadout => loadout.InstallationInstance)
                .OnUI()
                .Transform(installation =>
                {
                    var vm = provider.GetRequiredService<IGameWidgetViewModel>();
                    vm.Installation = installation;

                    vm.RemoveAllLoadoutsCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        if (GetJobRunningForGameInstallation(installation).IsT2) return;

                        vm.State = GameWidgetState.RemovingGame;
                        await Task.Run(async () => await RemoveAllLoadouts(installation));
                        vm.State = GameWidgetState.ManagedGame;
                    });

                    vm.ViewGameCommand = ReactiveCommand.Create(() => { NavigateToFirstLoadout(conn, installation); });

                    var job = GetJobRunningForGameInstallation(installation);
                    vm.State = job.IsT2 ? GameWidgetState.RemovingGame : GameWidgetState.ManagedGame;

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

                    var job = GetJobRunningForGameInstallation(install);
                    vm.State = job.IsT1 ? GameWidgetState.AddingGame : GameWidgetState.DetectedGame;

                    return vm;
                })
                .Bind(out _detectedGames)
                .SubscribeWithErrorLogging()
                .DisposeWith(d);
        });
    }

    private OneOf<None, JobId, JobId> GetJobRunningForGameInstallation(GameInstallation installation)
    {
        foreach (var job in CreateLoadoutJob.AllRunning(_jobManager))
        {
            if (job.Arg1.Equals(installation))
            {
                return OneOf<None, JobId, JobId>.FromT1(job.Id);
            }
        }
        
        foreach (var job in UnmanageGameJob.AllRunning(_jobManager))
        {
            if (job.Arg1.Equals(installation))
            {
                return OneOf<None, JobId, JobId>.FromT2(job.Id);
            }
        }
        
        return OneOf<None, JobId, JobId>.FromT0(new None());
    }

    private async Task RemoveAllLoadouts(GameInstallation installation)
    {
        await installation.GetGame().Synchronizer.UnManage(installation);
    }

    private async Task ManageGame(GameInstallation installation)
    {
        await installation.GetGame().Synchronizer.CreateLoadout(installation);
    }

    private void NavigateToFirstLoadout(IConnection conn, GameInstallation installation)
    {
        var db = conn.Db;
        
        var loadout = Loadout.All(db).FirstOrOptional(loadout => loadout.IsVisible() 
                                                                 && loadout.InstallationInstance.Equals(installation));
        if (!loadout.HasValue)
        {
            return;
        }

        var loadoutId = loadout.Value.LoadoutId;
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
