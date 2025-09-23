using System.ComponentModel;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using ObservableCollections;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Downloads;

public class DownloadsPageViewModel : APageViewModel<IDownloadsPageViewModel>, IDownloadsPageViewModel
{
    [Reactive] public int SelectionCount { get; private set; } = 0;
    
    [Reactive] public bool IsEmptyStateActive { get; set; } = true;
    
    public string HeaderTitle { get; private set; } = string.Empty;
    public string HeaderDescription { get; private set; } = string.Empty;

    public DownloadsTreeDataGridAdapter Adapter { get; }
    public ReactiveCommand<Unit> PauseAllCommand { get; }
    public ReactiveCommand<Unit> ResumeAllCommand { get; }
    public ReactiveCommand<Unit> PauseSelectedCommand { get; }
    public ReactiveCommand<Unit> ResumeSelectedCommand { get; }
    public ReactiveCommand<Unit> CancelSelectedCommand { get; }
    public Observable<bool> SelectionHasRunningItems { get; }
    public Observable<bool> SelectionHasPausedItems { get; }
    public Observable<bool> SelectionHasActiveItems { get; }
    public Observable<bool> HasRunningItems { get; }
    public Observable<bool> HasPausedItems { get; }

    public DownloadsPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, DownloadsPageContext context) : base(windowManager)
    {
        var downloadsService = serviceProvider.GetRequiredService<IDownloadsService>();

        // Create filter based on context
        var filter = context switch
        {
            AllDownloadsPageContext => DownloadsFilter.All(),
            GameSpecificDownloadsPageContext g => DownloadsFilter.ForGame(g.GameId),
            _ => DownloadsFilter.Active(),
        };

        var downloadsDataProvider = serviceProvider.GetRequiredService<IDownloadsDataProvider>();
        Adapter = new DownloadsTreeDataGridAdapter(downloadsDataProvider, filter);
        
        TabTitle = Language.Downloads_WorkspaceTitle;
        TabIcon = IconValues.PictogramDownload;
        
        // Set header title and description based on context
        (HeaderTitle, HeaderDescription) = context switch
        {
            AllDownloadsPageContext => (Language.DownloadsLeftMenu_AllDownloads, Language.DownloadsPage_AllDownloads_Description),
            GameSpecificDownloadsPageContext g => (string.Format(Language.DownloadsLeftMenu_GameSpecificDownloads, downloadsDataProvider.ResolveGameName(g.GameId)), Language.DownloadsPage_GameSpecificDownloads_Description),
            _ => (Language.InProgressTitleTextBlock, Language.DownloadsPage_Default_Description)
        };

        var runningCountChanges = downloadsService.GetDownloadsByStatus(JobStatus.Running)
            // NOTE(sewer): This is a hack.
            //
            // When we pause/resume a selected item, there's a chance that GetDownloadsByStatus
            // will fire before the status of the actual job changes. This means that the evaluation
            // result will not be correct, so we need to delay for a short moment to make sure it is.
            // 
            // As far as workarounds go, we can't subscribe to DownloadComponents.StatusComponent directly because
            // the JobStatus field there is derived from the model, i.e. also from GetDownloadsByStatus(JobStatus.Running)
            //
            // We can't guarantee a specific execution order; so we wait with a small hack.
            .Delay(TimeSpan.FromMilliseconds(64))
            .OnUI()
            .Select(_ => Unit.Default);
        
        // Create observables that track selection and status changes
        var selectionStatusReevaluationTrigger = this.WhenAnyValue(vm => vm.SelectionCount)
            .Select(_ => Unit.Default)
            .Merge(runningCountChanges);

        SelectionHasRunningItems = selectionStatusReevaluationTrigger
            .Select(_ => Adapter.SelectedModels
                .Select(model => model.GetOptional<DownloadComponents.StatusComponent>(DownloadColumns.Status.ComponentKey))
                .Where(opt => opt.HasValue)
                .Any(opt => opt.Value.Status.Value == JobStatus.Running))
            .ToObservable();
        
        SelectionHasPausedItems = selectionStatusReevaluationTrigger
            .Select(_ => Adapter.SelectedModels
                .Select(model => model.GetOptional<DownloadComponents.StatusComponent>(DownloadColumns.Status.ComponentKey))
                .Where(opt => opt.HasValue)
                .Any(opt => opt.Value.Status.Value == JobStatus.Paused))
            .ToObservable();
        
        SelectionHasActiveItems = selectionStatusReevaluationTrigger
            .Select(_ => Adapter.SelectedModels
                .Select(model => model.GetOptional<DownloadComponents.StatusComponent>(DownloadColumns.Status.ComponentKey))
                .Where(opt => opt.HasValue)
                .Any(opt => opt.Value.Status.Value.IsActive()))
            .ToObservable();
        
        HasRunningItems = downloadsService.GetDownloadsByStatus(JobStatus.Running)
            .QueryWhenChanged(items => items.Count > 0)
            .OnUI()
            .ToObservable()
            .Prepend(false);
        
        HasPausedItems = downloadsService.GetDownloadsByStatus(JobStatus.Paused)
            .QueryWhenChanged(items => items.Count > 0)
            .OnUI()
            .ToObservable()
            .Prepend(false);

        // Commands with CanExecute
        PauseAllCommand = HasRunningItems.ToReactiveCommand<Unit>(_ => downloadsService.PauseAll());
        ResumeAllCommand = HasPausedItems.ToReactiveCommand<Unit>(_ => downloadsService.ResumeAll());
        
        PauseSelectedCommand = SelectionHasRunningItems.ToReactiveCommand<Unit>(
            executeAsync: (_, _) =>
            {
                foreach (var model in Adapter.SelectedModels)
                {
                    var downloadRef = model.GetOptional<DownloadRef>(DownloadColumns.DownloadRefComponentKey);
                    var statusComponent = model.GetOptional<DownloadComponents.StatusComponent>(DownloadColumns.Status.ComponentKey);
                    
                    // Only pause downloads that are currently running
                    if (downloadRef.HasValue && statusComponent is { HasValue: true, Value.Status.Value: JobStatus.Running })
                        downloadsService.PauseDownload(downloadRef.Value.Download);
                }

                return ValueTask.CompletedTask;
            },
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );
        
        ResumeSelectedCommand = SelectionHasPausedItems.ToReactiveCommand<Unit>(
            executeAsync: (_, _) =>
            {
                foreach (var model in Adapter.SelectedModels)
                {
                    var downloadRef = model.GetOptional<DownloadRef>(DownloadColumns.DownloadRefComponentKey);
                    var statusComponent = model.GetOptional<DownloadComponents.StatusComponent>(DownloadColumns.Status.ComponentKey);
                    
                    // Only resume downloads that are currently paused
                    if (downloadRef.HasValue && statusComponent.HasValue && statusComponent.Value.Status.Value == JobStatus.Paused)
                        downloadsService.ResumeDownload(downloadRef.Value.Download);
                }

                return ValueTask.CompletedTask;
            },
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        CancelSelectedCommand = SelectionHasActiveItems.ToReactiveCommand<Unit>(
            executeAsync: (_, _) =>
            {
                foreach (var model in Adapter.SelectedModels)
                {
                    var downloadRef = model.GetOptional<DownloadRef>(DownloadColumns.DownloadRefComponentKey);
                    var statusComponent = model.GetOptional<DownloadComponents.StatusComponent>(DownloadColumns.Status.ComponentKey);
                    
                    // Only cancel downloads that are currently active (running or paused)
                    if (downloadRef.HasValue && statusComponent.HasValue && statusComponent.Value.Status.Value.IsActive())
                        downloadsService.CancelDownload(downloadRef.Value.Download);
                }

                return ValueTask.CompletedTask;
            },
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        this.WhenActivated(disposables =>
        {
            Adapter.Activate().AddTo(disposables);
            
            // Track selection count using count property
            Adapter.SelectedModels
                .ObserveCountChanged(notifyCurrentCount: true)
                .Subscribe(count => SelectionCount = count)
                .AddTo(disposables);

            // Track empty state
            Adapter.IsSourceEmpty
                .Subscribe(isEmpty => IsEmptyStateActive = isEmpty)
                .AddTo(disposables);

            // Subscribe to adapter messages for individual download actions
            Adapter.MessageSubject.Subscribe(
                (message) =>
                {
                    message.Switch(
                        pauseMessage =>
                        {
                            foreach (var download in pauseMessage.Downloads)
                                downloadsService.PauseDownload(download);
                        },
                        resumeMessage =>
                        {
                            foreach (var download in resumeMessage.Downloads)
                                downloadsService.ResumeDownload(download);
                        },
                        cancelMessage =>
                        {
                            foreach (var download in cancelMessage.Downloads)
                                downloadsService.CancelDownload(download);
                        }
                    );
                }
            ).AddTo(disposables);
        });
    }
}
