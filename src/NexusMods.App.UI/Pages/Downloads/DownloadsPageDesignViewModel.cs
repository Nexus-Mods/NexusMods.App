using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using R3;

namespace NexusMods.App.UI.Pages.Downloads;

public class DownloadsPageDesignViewModel : APageViewModel<IDownloadsPageViewModel>, IDownloadsPageViewModel
{
    public DownloadsTreeDataGridAdapter Adapter { get; }
    public int SelectionCount => 5;
    public Observable<bool> HasRunningItems { get; } = Observable.Return(true);
    public Observable<bool> HasPausedItems { get; } = Observable.Return(true);
    
    public bool IsEmptyStateActive { get; set; } = true;

    public ReactiveCommand<Unit> PauseAllCommand { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> ResumeAllCommand { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CancelSelectedCommand { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> PauseSelectedCommand { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> ResumeSelectedCommand { get; } = new ReactiveCommand();

    public DownloadsPageDesignViewModel() : base(new DesignWindowManager())
    {
        // Create a dummy adapter for design-time
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();
        
        Adapter = new DownloadsTreeDataGridAdapter(serviceProvider, DownloadsFilter.All());
        
        TabTitle = Language.Downloads_WorkspaceTitle;
        TabIcon = IconValues.PictogramDownload;
    }
}