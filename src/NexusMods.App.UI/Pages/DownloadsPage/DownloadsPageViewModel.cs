using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.DownloadsPage;

public class DownloadsPageViewModel : APageViewModel<IDownloadsPageViewModel>, IDownloadsPageViewModel
{
    private readonly IDownloadsService _downloadsService;

    [Reactive] public int SelectionCount { get; private set; } = 0;

    public ReactiveCommand<Unit> PauseAllCommand { get; }
    public ReactiveCommand<Unit> ResumeAllCommand { get; }
    public ReactiveCommand<Unit> CancelSelectedCommand { get; }

    public DownloadsPageViewModel(IWindowManager windowManager, IDownloadsService downloadsService) : base(windowManager)
    {
        _downloadsService = downloadsService;
        
        TabTitle = Language.Downloads_WorkspaceTitle;
        TabIcon = IconValues.PictogramDownload;

        // TODO: Implement command behaviors
        PauseAllCommand = new ReactiveCommand<Unit>(_ => { });
        ResumeAllCommand = new ReactiveCommand<Unit>(_ => { });
        CancelSelectedCommand = new ReactiveCommand<Unit>(_ => { });

        // TODO: Add WhenActivated block for reactive subscriptions
    }
}