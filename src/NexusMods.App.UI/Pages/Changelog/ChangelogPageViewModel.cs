using System.Reactive.Disposables;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Changelog;

[UsedImplicitly]
public class ChangelogPageViewModel : APageViewModel<IChangelogPageViewModel>, IChangelogPageViewModel
{
    private readonly Uri _changelogUri = new("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/main/CHANGELOG.md");

    [Reactive] public Version? TargetVersion { get; set; }
    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    public ChangelogPageViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager) : base(windowManager)
    {
        MarkdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();

        this.WhenActivated(disposables =>
        {
            var workspaceController = GetWorkspaceController();
            workspaceController.SetTabTitle(Language.ChangelogPageViewModel_ChangelogPageViewModel_Changelog, WorkspaceId, PanelId, TabId);
            workspaceController.SetIcon(IconValues.FileDocumentOutline, WorkspaceId, PanelId, TabId);

            MarkdownRendererViewModel.MarkdownUri = _changelogUri;
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }
}
