using NexusMods.Networking.Downloaders.Interfaces;

namespace NexusMods.App.UI.RightContent.Downloads.ViewModels;

/// <summary>
/// ViewModel for an individual download task displayed in the UI.
/// </summary>
public interface IDownloadTaskViewModel : IRightContentViewModel
{
    /// <summary>
    /// e.g. 'My Cool Mod'
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// e.g. '1.0.0'
    /// </summary>
    public string Version { get; }
    
    /// <summary>
    /// e.g. 'Skyrim'
    /// </summary>
    public string Game { get; }

    /// <summary>
    /// e.g. 'Downloading'
    /// </summary>
    public DownloadTaskStatus Status { get; }
    
    /// <summary>
    /// e.g. '0'
    /// </summary>
    public long DownloadedBytes { get; }
    
    /// <summary>
    /// e.g. '1024'
    /// </summary>
    public long SizeBytes { get; }
    
    /// <summary>
    /// Current download speed of this task in bytes per second.
    /// </summary>
    public long Throughput { get; }

    /// <summary>
    /// Schedules a cancellation of the task.
    /// </summary>
    public void Cancel()
    {
        
    }
}
