using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

[UsedImplicitly]
public partial class MarkdownRendererView : ReactiveUserControl<IMarkdownRendererViewModel>
{
    public MarkdownRendererView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            var viewModel = ViewModel!;

            MarkdownScrollViewer.Plugins.HyperlinkCommand = viewModel.OpenLinkCommand;
            MarkdownScrollViewer.Plugins.PathResolver = viewModel.PathResolver;
            MarkdownScrollViewer.Plugins.Plugins.Add(viewModel.ImageResolverPlugin);

            this.OneWayBind(ViewModel, vm => vm.Contents, view => view.MarkdownScrollViewer.Markdown)
                .DisposeWith(disposables);
        });
    }
}
