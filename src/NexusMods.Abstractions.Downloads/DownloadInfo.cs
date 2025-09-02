using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

[assembly: InternalsVisibleTo("NexusMods.Library")]

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents download information for UI display.
/// Thread safety is not guaranteed - UI consumers should use OnUI() or similar.
/// </summary>
[PublicAPI]
public class DownloadInfo : ReactiveObject
{
    /// <summary>
    /// Unique identifier for this download (DownloadId).
    /// </summary>
    public required DownloadId Id { get; init; }
    
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
    
    /// <summary>
    /// Entity ID of the file metadata associated with this download.
    /// </summary>
    /// <remarks>
    /// We store the EntityId rather than NexusModsFileMetadata.ReadOnly to avoid circular dependency issues.
    /// The full FileMetadata can be retrieved from the database using this ID.
    /// </remarks>
    [Reactive] public required EntityId FileMetadataId { get; set; }
    
    /// <summary>
    /// Internal subscription management for reactive updates.
    /// </summary>
    internal CompositeDisposable? Subscriptions { get; set; }
}
