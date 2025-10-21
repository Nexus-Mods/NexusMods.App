using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents a Nexus Mods download job with associated metadata.
/// </summary>
public interface INexusModsDownloadJob : IDownloadJob
{
    /// <summary>
    /// Gets the underlying HTTP download job task.
    /// </summary>
    IJobTask<IHttpDownloadJob, AbsolutePath> HttpDownloadJob { get; }
    
    /// <summary>
    /// Gets the Nexus Mods file metadata associated with this download.
    /// </summary>
    NexusModsFileMetadata.ReadOnly FileMetadata { get; }
    
    // Note: AbsolutePath Destination is inherited from IDownloadJob
}
