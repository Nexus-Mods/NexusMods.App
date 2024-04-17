using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

public partial class MarkdownRendererView : ReactiveUserControl<IMarkdownRendererViewModel>
{
    public MarkdownRendererView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Contents, view => view.MarkdownScrollViewer.Markdown)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.OpenLinkCommand, view => view.MarkdownScrollViewer.Engine.HyperlinkCommand)
                .DisposeWith(disposables);
        });
    }
}

