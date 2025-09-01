using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents download information for UI display.
/// </summary>
[PublicAPI]
public class DownloadInfo : ReactiveObject
{
    /// <summary>
    /// Unique identifier for this download.
    /// </summary>
    public required DownloadId Id { get; init; }
    
    /// <summary>
    /// The job ID for this download. Only valid for active downloads.
    /// Consumers should generally use Id instead, but this is available for service implementations.
    /// </summary>
    public Optional<JobId> JobId { get; set; }
    
    /// <summary>
    /// Display name of the download (typically mod name).
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The game this download is for.
    /// </summary>
    [Reactive] public required GameId GameId { get; set; }
    
    /// <summary>
    /// Total file size.
    /// </summary>
    [Reactive] public Size FileSize { get; set; }
    
    /// <summary>
    /// Download progress percentage.
    /// </summary>
    [Reactive] public Percent Progress { get; set; }
    
    /// <summary>
    /// Downloaded bytes so far.
    /// </summary>
    [Reactive] public Size DownloadedBytes { get; set; }
    
    /// <summary>
    /// Current transfer rate.
    /// </summary>
    [Reactive] public Size TransferRate { get; set; }
    
    /// <summary>
    /// Current job status.
    /// </summary>
    [Reactive] public JobStatus Status { get; set; }
    
    /// <summary>
    /// The page URL where the download originated.
    /// </summary>
    [Reactive] public Optional<Uri> DownloadPageUri { get; set; }
    
    /// <summary>
    /// When the download completed.
    /// </summary>
    [Reactive] public Optional<DateTimeOffset> CompletedAt { get; set; }
}
