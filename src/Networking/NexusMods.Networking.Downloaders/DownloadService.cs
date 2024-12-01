using System.Collections.ObjectModel;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using System.Reactive.Disposables;
using DynamicData.Kernel;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <inheritdoc cref="IDownloadService"/>
[Obsolete(message: "To be replaced with ILibraryService")]
public class DownloadService : IDownloadService, IDisposable, IHostedService
{
    /// <inheritdoc />
    public ReadOnlyObservableCollection<IDownloadTask> Downloads => _downloadsCollection;
    
    /// <inheritdoc />
    public AbsolutePath OngoingDownloadsDirectory { get; private set; }
    private ReadOnlyObservableCollection<IDownloadTask> _downloadsCollection = ReadOnlyObservableCollection<IDownloadTask>.Empty;

    private readonly SourceCache<IDownloadTask, EntityId> _downloads = new(t => default!);
    private readonly ILogger<DownloadService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IConnection _conn;
    private bool _isDisposed;
    private readonly CompositeDisposable _disposables;
    private readonly IFileStore _fileStore;

    public DownloadService(
        ILogger<DownloadService> logger, 
        IServiceProvider provider, 
        IFileStore fileStore, 
        IConnection conn,
        IFileSystem fs,
        ISettingsManager settingsManager)
    {
        _logger = logger;
        _provider = provider;
        _conn = conn;
        _disposables = new CompositeDisposable();
        _fileStore = fileStore;
        OngoingDownloadsDirectory = settingsManager.Get<DownloadSettings>().OngoingDownloadLocation.ToPath(fs);
        if (!OngoingDownloadsDirectory.DirectoryExists())
        {
            OngoingDownloadsDirectory.CreateDirectory();
        }
    }
    
    /// <inheritdoc />
    public async Task<IDownloadTask> AddTask(NXMModUrl url)
    {
        throw new NotSupportedException("This method is not supported for this service.");
    }

    /// <inheritdoc />
    public async Task<IDownloadTask> AddTask(Uri url)
    {
        throw new NotSupportedException("This method is not supported for this service.");

    }

    /// <inheritdoc />
    public Size GetThroughput()
    {
        return Size.Zero;
    }

    /// <inheritdoc />
    public async Task SetIsHidden(bool isHidden, IDownloadTask[] targets)
    {
        using var tx = _conn.BeginTransaction();
        foreach (var downloadTask in targets)
        {
            downloadTask.SetIsHidden(isHidden, tx);
        }
        await tx.Commit();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        
        _disposables.Dispose();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
}
