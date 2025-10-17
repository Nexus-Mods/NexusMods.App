using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.Library.Jobs;

/// <summary>
/// A job that adds a download job to the library after the download has been completed
/// </summary>
public interface IAddDownloadJob : IJobDefinition<LibraryFile.ReadOnly>
{
    /// <summary>
    /// The download job
    /// </summary>
    public IJobTask<IDownloadJob, AbsolutePath> DownloadJob { get; init; }
}
