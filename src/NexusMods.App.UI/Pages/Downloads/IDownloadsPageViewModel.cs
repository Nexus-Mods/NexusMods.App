using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.Downloads;

public interface IDownloadsPageViewModel : IPageViewModelInterface
{
    public int SelectionCount { get; }
    public ReactiveCommand<Unit> PauseAllCommand { get; }
    public ReactiveCommand<Unit> ResumeAllCommand { get; }
    public ReactiveCommand<Unit> CancelSelectedCommand { get; }
    public bool IsEmptyStateActive { get; set; }
}