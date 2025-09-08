using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.DownloadsPage;

public interface IDownloadsPageViewModel : IPageViewModelInterface
{
    public int SelectionCount { get; }
    public ReactiveCommand<Unit> PauseAllCommand { get; }
    public ReactiveCommand<Unit> ResumeAllCommand { get; }
    public ReactiveCommand<Unit> CancelSelectedCommand { get; }
}