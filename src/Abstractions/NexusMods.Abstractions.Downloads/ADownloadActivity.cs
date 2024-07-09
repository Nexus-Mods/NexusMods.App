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
        AbsolutePath downloadPath)
    {
        PersistedStateId = persistedState.PersistedDownloadStateId;
        Status = persistedState.Status;

        Downloader = downloader;
        Title = persistedState.Title;
        Downloader = downloader;
    }

    /// <inheritdoc/>
    public PersistedDownloadStatus Status { get; private set; }

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
    /// Sets the current status.
    /// </summary>
    /// <remarks>
    /// This updates <see cref="PersistedDownloadState.Status"/> for <see cref="PersistedStateId"/>.
    /// </remarks>
    public void SetStatus(ITransaction tx, PersistedDownloadStatus status)
    {
        Status = status;
        tx.Add(PersistedStateId, PersistedDownloadState.Status, status);

        this.RaisePropertyChanged(nameof(Status));
    }

    public override string ToString() => Title;
}
