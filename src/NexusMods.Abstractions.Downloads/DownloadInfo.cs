using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;

[assembly: InternalsVisibleTo("NexusMods.Library")]
[assembly: InternalsVisibleTo("NexusMods.Library.Tests")]

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents download information for UI display.
/// Thread safety is not guaranteed - UI consumers should use OnUI() or similar.
/// </summary>
[PublicAPI]
public class DownloadInfo : IDisposable
{
    /// <remarks>
    ///     The value of this field is <see cref="IJob.Id"/> for the 'NexusModsDownloadJob'.
    ///     We're trusting that GUIDs are unique enough to never (in practice) make a duplicate across multiple runs/reboots. 
    /// </remarks>
    public required DownloadId Id { get; init; }
    
    // Backing fields for reactive properties
    private readonly BindableReactiveProperty<string> _name = new(string.Empty);
    private readonly BindableReactiveProperty<GameId> _gameId = new(default(GameId));
    private readonly BindableReactiveProperty<Size> _fileSize = new(Size.From(0));
    private readonly BindableReactiveProperty<Percent> _progress = new(Percent.Zero);
    private readonly BindableReactiveProperty<Size> _downloadedBytes = new(Size.From(0));
    private readonly BindableReactiveProperty<Size> _transferRate = new(Size.From(0));
    private readonly BindableReactiveProperty<JobStatus> _status = new(default(JobStatus));
    private readonly BindableReactiveProperty<Optional<Uri>> _downloadPageUri = new(default(Optional<Uri>));
    private readonly BindableReactiveProperty<Optional<DateTimeOffset>> _completedAt = new(default(Optional<DateTimeOffset>));
    private readonly BindableReactiveProperty<EntityId> _fileMetadataId = new(default(EntityId));
    
    /// <summary>
    /// Display name of the download (typically mod name).
    /// </summary>
    public IReadOnlyBindableReactiveProperty<string> Name => _name;
    
    /// <summary>
    /// The game this download is for.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<GameId> GameId => _gameId;
    
    /// <summary>
    /// Total file size.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<Size> FileSize => _fileSize;
    
    /// <summary>
    /// Download progress percentage.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<Percent> Progress => _progress;
    
    /// <summary>
    /// Downloaded bytes so far.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<Size> DownloadedBytes => _downloadedBytes;
    
    /// <summary>
    /// Current transfer rate.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<Size> TransferRate => _transferRate;
    
    /// <summary>
    /// Current job status.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<JobStatus> Status => _status;
    
    /// <summary>
    /// The page URL where the download originated.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<Optional<Uri>> DownloadPageUri => _downloadPageUri;
    
    /// <summary>
    /// When the download completed.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<Optional<DateTimeOffset>> CompletedAt => _completedAt;
    
    /// <summary>
    /// Entity ID of the file metadata associated with this download.
    /// </summary>
    /// <remarks>
    /// We store the EntityId rather than NexusModsFileMetadata.ReadOnly to avoid circular dependency issues.
    /// The full FileMetadata can be retrieved from the database using this ID.
    /// </remarks>
    public IReadOnlyBindableReactiveProperty<EntityId> FileMetadataId => _fileMetadataId;
    
    /// <summary>
    /// Internal subscription management for reactive updates.
    /// </summary>
    internal System.Reactive.Disposables.CompositeDisposable? Subscriptions { get; set; }

    // Internal mutation methods for DownloadsService
    internal void SetName(string value) => _name.Value = value;
    internal void SetGameId(GameId value) => _gameId.Value = value;
    internal void SetFileSize(Size value) => _fileSize.Value = value;
    internal void SetProgress(Percent value) => _progress.Value = value;
    internal void SetDownloadedBytes(Size value) => _downloadedBytes.Value = value;
    internal void SetTransferRate(Size value) => _transferRate.Value = value;
    internal void SetStatus(JobStatus value) => _status.Value = value;
    internal void SetDownloadPageUri(Optional<Uri> value) => _downloadPageUri.Value = value;
    internal void SetCompletedAt(Optional<DateTimeOffset> value) => _completedAt.Value = value;
    internal void SetFileMetadataId(EntityId value) => _fileMetadataId.Value = value;

    private bool _isDisposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Subscriptions?.Dispose();
                _name?.Dispose();
                _gameId?.Dispose();
                _fileSize?.Dispose();
                _progress?.Dispose();
                _downloadedBytes?.Dispose();
                _transferRate?.Dispose();
                _status?.Dispose();
                _downloadPageUri?.Dispose();
                _completedAt?.Dispose();
                _fileMetadataId?.Dispose();
            }

            _isDisposed = true;
        }
    }
}
