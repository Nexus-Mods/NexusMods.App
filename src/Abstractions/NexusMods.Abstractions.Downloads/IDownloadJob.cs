using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents the work associated with downloading a single file.
/// </summary>
[PublicAPI]
public interface IDownloadJob : IPersistedJob
{
    /// <summary>
    /// Gets the path where the file will be downloaded to.
    /// </summary>
    AbsolutePath DownloadPath { get; }
}
