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
            this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Contents, view => view.MarkdownScrollViewer.Markdown)
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IMarkdownRendererViewModel viewModel)
    {
        MarkdownScrollViewer.Engine.HyperlinkCommand = viewModel.OpenLinkCommand;
        MarkdownScrollViewer.Engine.Plugins.PathResolver = viewModel.PathResolver;
        MarkdownScrollViewer.Engine.Plugins.Info.Register(viewModel.ImageResolver);
    }
}
