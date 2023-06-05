using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Interfaces;

/// <summary>
/// Represents an individual task to download and install a mod.
/// </summary>
public interface IDownloadTask
{
    /// <summary>
    /// Gets all download jobs associated with this task for the purpose of calculating throughput.
    /// </summary>
    IEnumerable<IJob<Size>> DownloadJobs { get; }
    
    /// <summary>
    /// Service this task is associated with.
    /// </summary>
    DownloadService Owner { get; }

    /// <summary>
    /// Starts executing the task.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Cancels a download task.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Pauses a download task.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a download task.
    /// </summary>
    void Resume();
}
