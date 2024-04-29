using System.Reactive.Disposables;
using System.Reactive.Linq;
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

        const string firstItem = "All Versions";
        ComboBox.ItemsSource = new[] { firstItem };

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.MarkdownRendererViewModel, view => view.ViewModelViewHost.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.ViewModel!.ParsedChangelog)
                .WhereNotNull()
                .Select(parsedChangelog => parsedChangelog.Versions
                    .Select(kv => kv.Key.ToString())
                    .Prepend(firstItem)
                    .ToArray()
                )
                .BindToView(this, view => view.ComboBox.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedIndex, view => view.ComboBox.SelectedIndex)
                .DisposeWith(disposables);
        });
    }
}
