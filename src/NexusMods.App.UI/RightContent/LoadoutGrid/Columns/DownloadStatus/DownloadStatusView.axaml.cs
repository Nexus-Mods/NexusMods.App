using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadStatus;

public partial class DownloadStatusView : ReactiveUserControl<IDownloadStatusViewModel>
{
    public DownloadStatusView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.CurrentValue)
                .BindToUi<float, DownloadStatusView, double>(this, view => view.DownloadProgressBar.Value)
                .DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.ViewModel!.Text)
                .BindToUi<string, DownloadStatusView, string>(this, view => view.DownloadProgressBar.ProgressTextFormat)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.ViewModel!.IsRunning)
                .OnUI()
                .Subscribe(isRunning =>
                {
                    // TODO: I (Sewer) am not particularly a fan of this; but I'm not sure of the best alternative for now.
                    if (isRunning)
                        DownloadProgressBar.Classes.Remove("DisabledDownloadBar");
                    else
                        DownloadProgressBar.Classes.Add("DisabledDownloadBar");
                })
                .DisposeWith(d);
        });
    }
}

