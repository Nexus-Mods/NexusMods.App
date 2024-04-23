using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Changelog;

[UsedImplicitly]
public partial class ChangelogPageView : ReactiveUserControl<IChangelogPageViewModel>
{
    public ChangelogPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.MarkdownRendererViewModel, view => view.ViewModelViewHost.ViewModel)
                .DisposeWith(disposables);
        });
    }
}
