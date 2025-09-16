using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.Downloads;

public interface IDownloadsPageViewModel : IPageViewModelInterface
{
    public DownloadsTreeDataGridAdapter Adapter { get; }
    public int SelectionCount { get; }
    public Observable<bool> HasRunningItems { get; }
    public Observable<bool> HasPausedItems { get; }
    public ReactiveCommand<Unit> PauseAllCommand { get; }
    public ReactiveCommand<Unit> ResumeAllCommand { get; }
    public ReactiveCommand<Unit> PauseSelectedCommand { get; }
    public ReactiveCommand<Unit> ResumeSelectedCommand { get; }
    public ReactiveCommand<Unit> CancelSelectedCommand { get; }
    public bool IsEmptyStateActive { get; set; }
    
    public string HeaderTitle { get; }
    public string HeaderDescription { get; }
}