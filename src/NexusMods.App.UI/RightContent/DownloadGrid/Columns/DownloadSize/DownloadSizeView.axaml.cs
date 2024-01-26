using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadSize;

public partial class DownloadSizeView : ReactiveUserControl<IDownloadSizeViewModel>
{
    public DownloadSizeView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.Size)
                .BindToUi<string, DownloadSizeView, string>(this, view => view.SizeTextBox.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

