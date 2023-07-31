using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads.ViewModels;

public class DownloadTaskViewModel : AViewModel<IDownloadTaskViewModel>, IDownloadTaskViewModel
{
    private readonly IDownloadTask _task;

    public DownloadTaskViewModel(IDownloadTask task, bool initPreviousStates = true)
    {
        _task = task;

        // Initialize the previous states
        if (!initPreviousStates)
            return;

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

    public long DownloadedBytes
    {
        get
        {
            if (_task.DownloadJob == null)
                return 0;

            return (long)_task.DownloadJob.Current.Value;
        }
    }

    public long SizeBytes
    {
        get
        {
            if (_task is IHaveFileSize size)
                return size.SizeBytes;

            return 0;
        }
    }

    public long Throughput
    {
        get
        {
            if (_task.DownloadJob == null)
                return 0;

            return (long)_task.DownloadJob.GetThroughput(DateTimeProvider.Instance).Value;
        }
    }

    public void Cancel() => _task.Cancel();
    public void Suspend() => _task.Suspend();
    public void Resume() => _task.Resume();

    // Polling implementation, for bridging the gap between a non-INotifyPropertyChanged implementation and
    // live-updating ViewModel.
    private string _previousName = string.Empty;
    private string _previousVersion = string.Empty;
    private string _previousGame = string.Empty;
    private DownloadTaskStatus _previousStatus = DownloadTaskStatus.Idle;
    private long _previousDownloadedBytes = 0;
    private long _previousSizeBytes = 0;
    private long _previousThroughput = 0;

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
