using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Sdk.Jobs;
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
    public ReactiveCommand<Unit> PauseAllCommand { get; private set; } = null!;
    public ReactiveCommand<Unit> ResumeAllCommand { get; private set; } = null!;
    public ReactiveCommand<Unit> PauseSelectedCommand { get; private set; } = null!;
    public ReactiveCommand<Unit> ResumeSelectedCommand { get; private set; } = null!;
    public ReactiveCommand<Unit> CancelSelectedCommand { get; private set; } = null!;
    public Observable<bool> SelectionHasRunningItems { get; private set; } = null!;
    public Observable<bool> SelectionHasPausedItems { get; private set; } = null!;
    public Observable<bool> SelectionHasActiveItems { get; private set; } = null!;
    public Observable<bool> HasRunningItems { get; private set; } = null!;
    public Observable<bool> HasPausedItems { get; private set; } = null!;

    public DownloadsPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, DownloadsPageContext context) : base(windowManager)
    {
        var downloadsService = serviceProvider.GetRequiredService<IDownloadsService>();

        // Create filter based on context
        var filter = context.GameScope.HasValue
            ? DownloadsFilter.ForGame(context.GameScope.Value)
            : DownloadsFilter.All();

        var downloadsDataProvider = serviceProvider.GetRequiredService<IDownloadsDataProvider>();
        Adapter = new DownloadsTreeDataGridAdapter(serviceProvider, downloadsDataProvider, filter);
        
        TabTitle = Language.Downloads_WorkspaceTitle;
        TabIcon = IconValues.PictogramDownload;
        
        // Set header title and description based on context
        (HeaderTitle, HeaderDescription) = context.GameScope.HasValue
            ? (string.Format(Language.DownloadsLeftMenu_GameSpecificDownloads, downloadsDataProvider.ResolveGameName(context.GameScope.Value)), Language.DownloadsPage_GameSpecificDownloads_Description)
            : (Language.DownloadsLeftMenu_AllDownloads, Language.DownloadsPage_AllDownloads_Description);

        // Create observables and commands
        var runningCountChanges = downloadsService.GetDownloadsByStatus(JobStatus.Running)
            // NOTE(sewer): This is a hack.
            // Explanation: https://github.com/Nexus-Mods/NexusMods.App/pull/3898#discussion_r2387773402
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
        
        // Create context-aware observables for running and paused items
        HasRunningItems = context.GameScope.HasValue
            ? downloadsService.GetRunningDownloadsForGame(context.GameScope.Value)
                .QueryWhenChanged(items => items.Count > 0)
                .OnUI()
                .ToObservable()
                .Prepend(false)
            : downloadsService.GetDownloadsByStatus(JobStatus.Running)
                .QueryWhenChanged(items => items.Count > 0)
                .OnUI()
                .ToObservable()
                .Prepend(false);
        
        HasPausedItems = context.GameScope.HasValue
            ? downloadsService.GetPausedDownloadsForGame(context.GameScope.Value)
                .QueryWhenChanged(items => items.Count > 0)
                .OnUI()
                .ToObservable()
                .Prepend(false)
            : downloadsService.GetDownloadsByStatus(JobStatus.Paused)
                .QueryWhenChanged(items => items.Count > 0)
                .OnUI()
                .ToObservable()
                .Prepend(false);

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

            // Commands with CanExecute - context-aware implementation
            PauseAllCommand = context.GameScope.HasValue
                ? HasRunningItems.ToReactiveCommand<Unit>(_ => downloadsService.PauseAllForGame(context.GameScope.Value)).AddTo(disposables)
                : HasRunningItems.ToReactiveCommand<Unit>(_ => downloadsService.PauseAll()).AddTo(disposables);
            
            ResumeAllCommand = context.GameScope.HasValue
                ? HasPausedItems.ToReactiveCommand<Unit>(_ => downloadsService.ResumeAllForGame(context.GameScope.Value)).AddTo(disposables)
                : HasPausedItems.ToReactiveCommand<Unit>(_ => downloadsService.ResumeAll()).AddTo(disposables);
            
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
            ).AddTo(disposables);
            
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
            ).AddTo(disposables);

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
            ).AddTo(disposables);
        });
    }
}
