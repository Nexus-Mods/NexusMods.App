using System.Reactive;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.AlphaWarning;

public interface IAlphaWarningViewModel : IOverlayViewModel
{
    public ReactiveCommand<Unit, Unit> ViewChangelogInAppCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenDiscordCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenForumsCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenGitHubCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public IWorkspaceController? WorkspaceController { get; set; }
    public bool MaybeShow();
}
