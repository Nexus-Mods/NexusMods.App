using NexusMods.Networking.Downloaders.Interfaces;

namespace NexusMods.App.UI.RightContent.Downloads.ViewModels;

public class DownloadTaskDesignViewModel : AViewModel<IDownloadTaskViewModel>, IDownloadTaskViewModel
{
    public string Name { get; set; } = "Design Mod";
    public string Version { get; set; } = "1.0.0";
    public string Game { get; set; } = "Unknown Game";
    public DownloadTaskStatus Status { get; set; } = DownloadTaskStatus.Idle;
    public long DownloadedBytes { get; set; } = 1024 * 1024 * 512;
    public long SizeBytes { get; set; } = 1024 * 1024 * 1337;
    public long Throughput { get; set; } = 0;
}
