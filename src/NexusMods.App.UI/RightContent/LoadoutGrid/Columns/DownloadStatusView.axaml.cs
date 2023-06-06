using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public partial class DownloadStatusView : ReactiveUserControl<IDownloadStatusViewModel>
{
    public DownloadStatusView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.CurrentValue)
                .BindToUi(this, view => view.DownloadProgressBar.Value)
                .DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.ViewModel!.Text)
                .BindToUi(this, view => view.DownloadProgressBar.ProgressTextFormat)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

