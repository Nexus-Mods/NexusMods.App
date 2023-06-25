using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads.ViewModels;

public class DownloadTaskViewModel : AViewModel<IDownloadTaskViewModel>, IDownloadTaskViewModel
{
    private readonly IDownloadTask _task;
    
    public DownloadTaskViewModel(IDownloadTask task)
    {
        _task = task;
        
        // Initialize the previous states
        _previousName = Name;
        _previousVersion = Version;
        _previousGame = Game;
        _previousStatus = Status;
        _previousDownloadedBytes = DownloadedBytes;
        _previousSizeBytes = SizeBytes;
        _previousThroughput = Throughput;
    }

    public string Name => _task.FriendlyName;
    public string Version 
    {
        get
        {
             if (_task is IHaveDownloadVersion version)
                 return version.Version;

             return "Unknown";
        }
    }
    
    public string Game 
    {
        get
        {
            if (_task is IHaveGameName name)
                return name.GameName;

            return "Unknown";
        }
    }

    public DownloadTaskStatus Status => _task.Status;

    public long DownloadedBytes => (long)_task.DownloadJobs.GetTotalCompletion().Value;
    
    public long SizeBytes 
    {
        get
        {
            if (_task is IHaveFileSize size)
                return size.SizeBytes;

            return 0;
        }
    }
    
    public long Throughput => (long)_task.DownloadJobs.GetTotalThroughput(DateTimeProvider.Instance).Value;
    public void Cancel() => _task.Cancel();

    // Polling implementation, for bridging the gap between a non-INotifyPropertyChanged implementation and 
    // live-updating ViewModel.
    private string _previousName;
    private string _previousVersion;
    private string _previousGame;
    private DownloadTaskStatus _previousStatus;
    private long _previousDownloadedBytes;
    private long _previousSizeBytes;
    private long _previousThroughput;
    
    public void Poll()
    {
        if (_previousName != Name)
        {
            _previousName = Name;
            this.RaisePropertyChanged(nameof(Name));
        }

        if (_previousVersion != Version)
        {
            _previousVersion = Version;
            this.RaisePropertyChanged(nameof(Version));
        }

        if (_previousGame != Game)
        {
            _previousGame = Game;
            this.RaisePropertyChanged(nameof(Game));
        }

        if (_previousStatus != Status)
        {
            _previousStatus = Status;
            this.RaisePropertyChanged(nameof(Status));
        }

        if (_previousDownloadedBytes != DownloadedBytes)
        {
            _previousDownloadedBytes = DownloadedBytes;
            this.RaisePropertyChanged(nameof(DownloadedBytes));
        }

        if (_previousSizeBytes != SizeBytes)
        {
            _previousSizeBytes = SizeBytes;
            this.RaisePropertyChanged(nameof(SizeBytes));
        }

        if (_previousThroughput != Throughput)
        {
            _previousThroughput = Throughput;
            this.RaisePropertyChanged(nameof(Throughput));
        }
    }
}
