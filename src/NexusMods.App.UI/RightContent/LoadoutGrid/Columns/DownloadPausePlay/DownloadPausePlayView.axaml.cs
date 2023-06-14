using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadPausePlay;

public partial class DownloadPausePlayView : ReactiveUserControl<IDownloadPausePlayViewModel>
{
    public DownloadPausePlayView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
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
            
            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

