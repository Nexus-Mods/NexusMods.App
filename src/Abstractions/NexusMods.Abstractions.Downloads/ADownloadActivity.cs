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

    public PersistedDownloadStateId PersistedStateId { get; }

    protected ADownloadActivity(PersistedDownloadState.ReadOnly persistedState)
    {
        _connection = persistedState.Db.Connection;

        PersistedStateId = persistedState.PersistedDownloadStateId;
        _status = persistedState.Status;
    }

    private PersistedDownloadStatus _status;
    public PersistedDownloadStatus Status
    {
        get => GetStatus();
        set => SetStatus(value);
    }

    [Reactive] public Size BytesTotal { get; set; }
    [Reactive] public Size BytesDownloaded { get; set; }
    [Reactive] public Size BytesRemaining { get; set; }
    [Reactive] public Percent Progress { get; set; }
    [Reactive] public Bandwidth Bandwidth { get; set; }

    public Task StartAsync()
    {
        SetStatus(PersistedDownloadStatus.Downloading);
        return StartInnerAsync();
    }

    public Task PauseAsync()
    {
        SetStatus(PersistedDownloadStatus.Paused);
        return PauseInnerAsync();
    }

    public Task CancelAsync()
    {
        SetStatus(PersistedDownloadStatus.Cancelled);
        return CancelInnerAsync();
    }

    protected abstract Task StartInnerAsync();
    protected abstract Task PauseInnerAsync();
    protected abstract Task CancelInnerAsync();

    protected PersistedDownloadStatus GetStatus() => _status;
    protected void SetStatus(PersistedDownloadStatus status, ITransaction? transaction = null)
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
}
