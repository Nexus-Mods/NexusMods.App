using System.Reactive;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.AlphaWarning;

public interface IAlphaWarningViewModel : IOverlayViewModel
{
    public ReactiveCommand<Unit, Unit> ViewChangelogInAppCommand { get; }
    public ReactiveCommand<Unit, Uri> ViewChangelogInBrowserCommand { get; }

    public ReactiveCommand<Unit, Uri> OpenDiscordCommand { get; }

    public ReactiveCommand<Unit, Uri> OpenForumsCommand { get; }

    public ReactiveCommand<Unit, Uri> OpenGitHubCommand { get; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public IWorkspaceController? WorkspaceController { get; set; }

    public bool MaybeShow();
}
