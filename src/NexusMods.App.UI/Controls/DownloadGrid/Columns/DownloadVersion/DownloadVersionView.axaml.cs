using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadVersion;

public partial class DownloadVersionView : ReactiveUserControl<IDownloadVersionViewModel>
{
    public DownloadVersionView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.Version)
                .BindToUi<string, DownloadVersionView, string>(this, view => view.VersionTextBox.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

