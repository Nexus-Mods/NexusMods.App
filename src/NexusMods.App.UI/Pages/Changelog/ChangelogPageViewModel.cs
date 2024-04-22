using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Changelog;

[UsedImplicitly]
public class ChangelogPageViewModel : APageViewModel<IChangelogPageViewModel>, IChangelogPageViewModel
{
    [Reactive] public Version? TargetVersion { get; set; }
    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    public ChangelogPageViewModel(IServiceProvider serviceProvider, IWindowManager windowManager) : base(windowManager)
    {
        MarkdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
    }
}
