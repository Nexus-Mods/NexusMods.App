using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;

namespace NexusMods.App.UI.RightContent.Downloads.ViewModels;

public class DownloadTaskViewModel : AViewModel<IDownloadTaskViewModel>, IDownloadTaskViewModel
{
    private readonly IDownloadTask _task;
    
    public DownloadTaskViewModel(IDownloadTask task) => _task = task;
    
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

}
