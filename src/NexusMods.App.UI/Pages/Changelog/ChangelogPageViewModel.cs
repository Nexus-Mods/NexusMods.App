using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Changelog;

[UsedImplicitly]
public class ChangelogPageViewModel : APageViewModel<IChangelogPageViewModel>, IChangelogPageViewModel
{
    private readonly Uri _changelogUri = new("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/main/CHANGELOG.md");

    private readonly HttpClient _client;

    [Reactive] public Version? TargetVersion { get; set; }
    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    public ChangelogPageViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager) : base(windowManager)
    {
        MarkdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();

        _client = serviceProvider.GetRequiredService<HttpClient>();

        var fetchChangelogCommand = ReactiveCommand.CreateFromTask(FetchChangelog);

        this.WhenActivated(disposables =>
        {
            fetchChangelogCommand
                .OnUI()
                .BindToVM(this, vm => vm.MarkdownRendererViewModel.Contents)
                .DisposeWith(disposables);

            fetchChangelogCommand
                .Execute()
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);
        });
    }

    private async Task<string> FetchChangelog(CancellationToken cancellationToken = default)
    {
        var changelog = await _client.GetStringAsync(_changelogUri, cancellationToken);
        return changelog;
    }
}
