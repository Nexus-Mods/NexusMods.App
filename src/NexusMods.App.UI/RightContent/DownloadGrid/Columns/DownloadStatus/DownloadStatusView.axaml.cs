using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadStatus;

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

            this.BindCommand(ViewModel, vm => vm.PauseOrResume, v => v.PlayPauseButton)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.ViewModel!.IsRunning)
                .OnUI()
                .Subscribe(isRunning =>
                {
                    // Note: This is inverse of BindToClasses, but this is the only use of this in the app for now, so not going into a helper.
                    if (isRunning)
                        DownloadProgressBar.Classes.Remove("DisabledDownloadBar");
                    else
                        DownloadProgressBar.Classes.Add("DisabledDownloadBar");
                })
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.ViewModel!.CanPause)
                .OnUI()
                .Subscribe(canPause =>
                {
                    // TODO: Not a fan of this either, but best we got.
                    const string playIcon = "PlayCircleOutline";
                    const string pauseIcon = "PauseCircleOutline";
                    var classes = PlayPauseIcon.Classes;

                    if (canPause)
                    {
                        classes.Remove(playIcon);
                        classes.Add(pauseIcon);
                    }
                    else
                    {
                        classes.Remove(pauseIcon);
                        classes.Add(playIcon);
                    }
                })
                .DisposeWith(d);
        });
    }
}

