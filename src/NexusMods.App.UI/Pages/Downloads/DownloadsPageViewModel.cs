using System.ComponentModel;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Downloads;

public class DownloadsPageViewModel : APageViewModel<IDownloadsPageViewModel>, IDownloadsPageViewModel
{
    [Reactive] public int SelectionCount { get; private set; } = 0;
    
    [Reactive] public bool IsEmptyStateActive { get; set; } = true;

    public DownloadsTreeDataGridAdapter Adapter { get; }
    public ReactiveCommand<Unit> PauseAllCommand { get; }
    public ReactiveCommand<Unit> ResumeAllCommand { get; }
    public ReactiveCommand<Unit> CancelSelectedCommand { get; }

    public DownloadsPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, DownloadsPageContext context) : base(windowManager)
    {
        var downloadsService = serviceProvider.GetRequiredService<IDownloadsService>();

        // Create filter based on context
        var filter = context switch
        {
            AllDownloadsPageContext => DownloadsFilter.All(),
            CompletedDownloadsPageContext => DownloadsFilter.Completed(),
            GameSpecificDownloadsPageContext g => DownloadsFilter.ForGame(g.GameId),
            _ => DownloadsFilter.Active()
        };

        Adapter = new DownloadsTreeDataGridAdapter(serviceProvider, filter);
        
        TabTitle = Language.Downloads_WorkspaceTitle;
        TabIcon = IconValues.PictogramDownload;

        // Commands
        PauseAllCommand = new ReactiveCommand<Unit>(_ => downloadsService.PauseAll());
        ResumeAllCommand = new ReactiveCommand<Unit>(_ => downloadsService.ResumeAll());
        
        var hasSelection = this.WhenAnyValue(vm => vm.SelectionCount)
            .ToObservable()
            .Select(count => count > 0);
        
        CancelSelectedCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, _) =>
            {
                foreach (var model in Adapter.SelectedModels)
                {
                    var downloadRef = model.GetOptional<DownloadRef>(DownloadColumns.DownloadRefComponentKey);
                    if (downloadRef.HasValue)
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
            System.Reactive.Linq.Observable.FromEventPattern<PropertyChangedEventArgs>(
                    Adapter.SelectedModels, nameof(Adapter.SelectedModels.CollectionChanged))
                .Select(_ => Adapter.SelectedModels.Count)
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
