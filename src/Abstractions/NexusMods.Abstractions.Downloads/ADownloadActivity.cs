using JetBrains.Annotations;
using NexusMods.Abstractions.Activities;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Implementation of <see cref="IDownloadActivity"/> for ease of use.
/// </summary>
[PublicAPI]
public abstract class ADownloadActivity : ReactiveObject, IDownloadActivity
{
    private readonly IConnection _connection;

    /// <inheritdoc/>
    public PersistedDownloadStateId PersistedStateId { get; }

    /// <inheritdoc/>
    public IDownloader Downloader { get; }

    /// <inheritdoc/>
    public string Title { get; }

    /// <inheritdoc/>
    public AbsolutePath DownloadPath { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ADownloadActivity(
        PersistedDownloadState.ReadOnly persistedState,
        IDownloader downloader,
        string title,
        AbsolutePath downloadPath)
    {
        _connection = persistedState.Db.Connection;

        PersistedStateId = persistedState.PersistedDownloadStateId;
        _status = persistedState.Status;

        Downloader = downloader;
        Title = title;
        Downloader = downloader;
    }

    private PersistedDownloadStatus _status;

    /// <inheritdoc/>
    public PersistedDownloadStatus Status
    {
        get => GetStatus();
        set => SetStatus(value);
    }

    /// <inheritdoc/>
    [Reactive] public Size BytesTotal { get; set; }

    /// <inheritdoc/>
    [Reactive] public Size BytesDownloaded { get; set; }

    /// <inheritdoc/>
    [Reactive] public Size BytesRemaining { get; set; }

    /// <inheritdoc/>
    [Reactive] public Percent Progress { get; set; }

    /// <inheritdoc/>
    [Reactive] public Bandwidth Bandwidth { get; set; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    protected virtual PersistedDownloadStatus GetStatus() => _status;

    /// <summary>
    /// Sets the current status.
    /// </summary>
    /// <remarks>
    /// This updates <see cref="PersistedDownloadState.Status"/> for <see cref="PersistedStateId"/>.
    /// </remarks>
    protected virtual void SetStatus(PersistedDownloadStatus status, ITransaction? transaction = null)
    {
        _status = status;

        var tx = transaction ?? _connection.BeginTransaction();
        try
        {
            tx.Add(PersistedStateId, PersistedDownloadState.Status, status);
            tx.Commit();
        }
        finally
        {
            if (transaction is null) tx.Dispose();
            this.RaisePropertyChanged(nameof(Status));
        }
    }

    public override string ToString() => Title;
}
