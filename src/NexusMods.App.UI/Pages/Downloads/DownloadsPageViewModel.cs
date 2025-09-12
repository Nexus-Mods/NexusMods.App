using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Sdk.Icons;
using OneOf;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Downloads;

public class DownloadsPageViewModel : APageViewModel<IDownloadsPageViewModel>, IDownloadsPageViewModel
{
    private readonly IDownloadsService _downloadsService;

    [Reactive] public int SelectionCount { get; private set; } = 0;
    
    [Reactive] public bool IsEmptyStateActive { get; set; } = true;

    public DownloadsTreeDataGridAdapter Adapter { get; }
    public ReactiveCommand<Unit> PauseAllCommand { get; }
    public ReactiveCommand<Unit> ResumeAllCommand { get; }
    public ReactiveCommand<Unit> CancelSelectedCommand { get; }

    public DownloadsPageViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, DownloadsPageContext context) : base(windowManager)
    {
        _downloadsService = serviceProvider.GetRequiredService<IDownloadsService>();
        
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
        PauseAllCommand = new ReactiveCommand<Unit>(_ => _downloadsService.PauseAll());
        ResumeAllCommand = new ReactiveCommand<Unit>(_ => _downloadsService.ResumeAll());
        
        var hasSelection = this.WhenAnyValue(vm => vm.SelectionCount)
            .ToObservable()
            .Select(count => count > 0);
        
        CancelSelectedCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) =>
            {
                var selectedDownloads = GetSelectedDownloads();
                _downloadsService.CancelRange(selectedDownloads);
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
            System.Reactive.Linq.Observable.FromEventPattern<System.ComponentModel.PropertyChangedEventArgs>(
                    Adapter.SelectedModels, nameof(Adapter.SelectedModels.CollectionChanged))
                .Select(_ => Adapter.SelectedModels.Count)
                .Subscribe(count => SelectionCount = count)
                .AddTo(disposables);

            // Track empty state
            Adapter.IsSourceEmpty
                .Subscribe(isEmpty => IsEmptyStateActive = isEmpty)
                .AddTo(disposables);

            // Subscribe to adapter messages for individual download actions
            Adapter.MessageSubject.SubscribeAwait(
                onNextAsync: async (message, cancellationToken) =>
                {
                    message.Switch(
                        pauseMessage =>
                        {
                            foreach (var download in pauseMessage.Downloads)
                            {
                                _downloadsService.PauseDownload(download);
                            }
                        },
                        resumeMessage =>
                        {
                            foreach (var download in resumeMessage.Downloads)
                            {
                                _downloadsService.ResumeDownload(download);
                            }
                        },
                        cancelMessage =>
                        {
                            foreach (var download in cancelMessage.Downloads)
                            {
                                _downloadsService.CancelDownload(download);
                            }
                        }
                    );
                }
            ).AddTo(disposables);
        });
    }

    private DownloadInfo[] GetSelectedDownloads()
    {
        var downloads = new List<DownloadInfo>();
        
        foreach (var model in Adapter.SelectedModels)
        {
            var downloadRef = model.GetOptional<DownloadRef>(DownloadColumns.DownloadRefComponentKey);
            if (downloadRef.HasValue)
            {
                downloads.Add(downloadRef.Value.Download);
            }
        }
        
        return downloads.ToArray();
    }
}