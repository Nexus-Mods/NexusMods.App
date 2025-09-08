using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using R3;

namespace NexusMods.App.UI.Pages.DownloadsPage;

public class DownloadsPageDesignViewModel : APageViewModel<IDownloadsPageViewModel>, IDownloadsPageViewModel
{
    public int SelectionCount => 5;

    public ReactiveCommand<Unit> PauseAllCommand { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> ResumeAllCommand { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CancelSelectedCommand { get; } = new ReactiveCommand();

    public DownloadsPageDesignViewModel() : base(new DesignWindowManager())
    {
        TabTitle = Language.Downloads_WorkspaceTitle;
        TabIcon = IconValues.PictogramDownload;
    }
}