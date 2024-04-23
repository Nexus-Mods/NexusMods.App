using System.Reactive.Disposables;
using System.Reactive.Linq;
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

            MarkdownScrollViewer.Engine.HyperlinkCommand = viewModel.OpenLinkCommand;
            MarkdownScrollViewer.Engine.Plugins.PathResolver = viewModel.PathResolver;
            MarkdownScrollViewer.Engine.Plugins.Plugins.Add(viewModel.ImageResolverPlugin);

            this.OneWayBind(ViewModel, vm => vm.Contents, view => view.MarkdownScrollViewer.Markdown)
                .DisposeWith(disposables);
        });
    }
}
