using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Changelog;

public interface IChangelogPageViewModel : IPageViewModelInterface
{
    public Version? TargetVersion { get; set; }

    public ParsedChangelog? ParsedChangelog { get; }

    public int SelectedIndex { get; set; }

    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }
}
