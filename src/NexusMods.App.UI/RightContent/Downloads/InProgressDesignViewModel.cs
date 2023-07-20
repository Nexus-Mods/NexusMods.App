using System.Reactive.Disposables;
using DynamicData;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressDesignViewModel : InProgressCommonViewModel
{
    private SourceList<IDownloadTaskViewModel> _tasks;
    
    public InProgressDesignViewModel()
    {
        _tasks = new();
        _tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Invisible Camouflage",
            Game = "Hide and Seek Pro",
            Version = "2.5.0",
            DownloadedBytes = 330_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Downloading,
            Throughput = 10_000_000
        });
        
        _tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Time Travel Mod",
            Game = "Chronos Unleashed",
            Version = "1.2.0",
            DownloadedBytes = 280_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Downloading,
            Throughput = 4_500_000
        });
        
        _tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Unlimited Lives",
            Game = "Endless Quest",
            Version = "13.3.7",
            DownloadedBytes = 100_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Paused
        });

        _tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Silent Karaoke Mode",
            Game = "Pop Star World",
            Version = "0.0.0",
            DownloadedBytes = 0
        });

        this.WhenActivated(d =>
        {
            _tasks.Connect()
                .Bind(out TasksObservable)
                .Subscribe()
                .DisposeWith(d);
            
            // This is necessary due to inheritance,
            // WhenActivated gets fired in wrong order and
            // parent classes need to be able to properly subscribe
            // here.
            this.RaisePropertyChanged(nameof(Tasks));
        });
    }

    public void AddDownload(DownloadTaskDesignViewModel vm) => _tasks.Add(vm);

    public void ClearDownloads() => _tasks.Clear();
}
