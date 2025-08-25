using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.Loadouts.Exceptions;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = System.Reactive.Linq.Observable;
using ReactiveCommand = ReactiveUI.ReactiveCommand;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    private readonly IConnection _conn;
    private readonly ISynchronizerService _syncService;
    private readonly IJobMonitor _jobMonitor;

    private readonly LoadoutId _loadoutId;
    private readonly IServiceProvider _serviceProvider;
    private readonly GameInstallMetadataId _gameMetadataId;
    [Reactive] private bool CanApply { get; set; } = true;
    [Reactive] public bool IsApplying { get; private set; }

    public ReactiveUI.ReactiveCommand<Unit, Unit> ApplyCommand { get; }
    public ReactiveUI.ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }

    [Reactive] public bool IsProcessing { get; private set; }
    [Reactive] public string ApplyButtonText { get; private set; } = Language.ApplyControlViewModel__APPLY;

    [Reactive] public string ProcessingText { get; private set; } = "";
    [Reactive] public bool IsLaunchButtonEnabled { get; private set; } = true;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }

    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider, IJobMonitor jobMonitor, IOverlayController overlayController, GameRunningTracker gameRunningTracker)
    {
        _loadoutId = loadoutId;
        _serviceProvider = serviceProvider;
        _syncService = serviceProvider.GetRequiredService<ISynchronizerService>();
        _conn = serviceProvider.GetRequiredService<IConnection>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        var windowManager = serviceProvider.GetRequiredService<IWindowManager>();
        
        _gameMetadataId = NexusMods.Abstractions.Loadouts.Loadout.Load(_conn.Db, loadoutId).InstallationId;

        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutId;
        
        ApplyCommand = ReactiveCommand.CreateFromTask(async () => await Apply(), 
            canExecute: this.WhenAnyValue(vm => vm.CanApply));
        
        ShowApplyDiffCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var pageData = new PageData
            {
                FactoryId = ApplyDiffPageFactory.StaticId,
                Context = new ApplyDiffPageContext
                {
                    LoadoutId = _loadoutId,
                },
            };

            var workspaceController = windowManager.ActiveWorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            var workspaceId = workspaceController.ActiveWorkspaceId;
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        });

        this.WhenActivated(disposables =>
            {
                var isProcessingObservable = _jobMonitor.HasActiveJob<ProcessLoadoutChangesJob>(job => job.LoadoutId.Equals(loadoutId))
                    .Prepend(false);
                
                var loadoutStatuses = Observable.Prepend(Observable.FromAsync(() => _syncService.StatusForLoadout(_loadoutId))
                        .Switch(), LoadoutSynchronizerState.Pending);

                var gameStatuses = _syncService.StatusForGame(_gameMetadataId)
                    .Prepend(GameSynchronizerState.Idle);

                // Note(sewer):
                // Fire an initial value with StartWith because CombineLatest requires all stuff to have latest values.
                // In any case, we should prevent Apply from being available while a file is in use.
                // A file may be in use because:
                // - The user launched the game externally (e.g. through Steam).
                //     - Approximate this by seeing if any EXE in any of the game folders are running.
                //     - This is done in 'Synchronize' method.
                // - They're running a tool from within the App.
                //     - Check running jobs.
                loadoutStatuses.CombineLatest(isProcessingObservable, gameStatuses, gameRunningTracker.GetWithCurrentStateAsStarting(), 
                        (loadout, isProcessing, game, running) => (loadout, isProcessing, game, running))
                    .OnUI()
                    .Subscribe(status =>
                    {
                        var (ldStatus, isProcessing,  gameStatus, running) = status;
                        
                        IsProcessing = isProcessing;
                        CanApply = !isProcessing
                                   && !running
                                   && gameStatus != GameSynchronizerState.Busy
                                   && ldStatus != LoadoutSynchronizerState.Pending
                                   && ldStatus != LoadoutSynchronizerState.Current;
                        IsLaunchButtonEnabled = !isProcessing 
                                                && !running
                                                && gameStatus != GameSynchronizerState.Busy
                                                && ldStatus == LoadoutSynchronizerState.Current;
                        
                    })
                    .DisposeWith(disposables);

                _jobMonitor.HasActiveJob<SynchronizeLoadoutJob>(job => job.LoadoutId == loadoutId)
                    .Prepend(_jobMonitor.Jobs.Any(job => job.Definition is SynchronizeLoadoutJob sJob && sJob.LoadoutId == loadoutId))
                    .OnUI()
                    .Subscribe(isApplying => IsApplying = isApplying)
                    .DisposeWith(disposables);
                
                _jobMonitor.ObserveActiveJobs<SynchronizeLoadoutJob>()
                    .Prepend(ChangeSet<IJob, JobId>.Empty)
                    .QueryWhenChanged(jobs =>
                        {
                            if (jobs.Items.FirstOrDefault()?.Definition is SynchronizeLoadoutJob sJob && sJob.LoadoutId == loadoutId)
                                return sJob.StatusMessage.AsSystemObservable();;
                            return new BindableReactiveProperty<string>(value: "NONE").AsSystemObservable();
                        }
                    ).Switch()
                    .OnUI()
                    .Subscribe(status => ProcessingText = status)
                    .DisposeWith(disposables);
            }
        );
    }

    private async Task Apply()
    {
        try
        {
            await Task.Run(async () =>
            {
                await _syncService.Synchronize(_loadoutId);
            });
        }
        catch (ExecutableInUseException)
        {
            var marker = NexusMods.Abstractions.Loadouts.Loadout.Load(_conn.Db, _loadoutId);
            await MessageBoxOkViewModel.ShowGameAlreadyRunningError(_serviceProvider, marker.Installation.Name);
        }
    }
}
