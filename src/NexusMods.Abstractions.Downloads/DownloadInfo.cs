using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
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
    /// The job ID for this download.
    /// </summary>
    public required JobId Id { get; init; }
    
    /// <summary>
    /// Display name of the download (typically mod name).
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The game this download is for.
    /// </summary>
    [Reactive] public Optional<GameId> GameId { get; set; }
    
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
    /// Estimated time remaining.
    /// </summary>
    [Reactive] public Optional<TimeSpan> EstimatedTimeRemaining { get; set; }
    
    /// <summary>
    /// Current transfer rate.
    /// </summary>
    [Reactive] public Size TransferRate { get; set; }
    
    /// <summary>
    /// Current job status.
    /// </summary>
    [Reactive] public JobStatus Status { get; set; }
    
    /// <summary>
    /// Source URL of the download.
    /// </summary>
    [Reactive] public Optional<Uri> DownloadUri { get; set; }
    
    /// <summary>
    /// The page URL where the download originated.
    /// </summary>
    [Reactive] public Optional<Uri> DownloadPageUri { get; set; }
    
    /// <summary>
    /// The completed library file if download is finished.
    /// </summary>
    [Reactive] public Optional<LibraryFile.ReadOnly> CompletedFile { get; set; }
    
    /// <summary>
    /// When the download started.
    /// </summary>
    [Reactive] public DateTimeOffset StartedAt { get; set; }
    
    /// <summary>
    /// When the download completed (if applicable).
    /// </summary>
    [Reactive] public Optional<DateTimeOffset> CompletedAt { get; set; }
}