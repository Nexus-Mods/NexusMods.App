using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Library;
using NexusMods.Paths;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="ILibraryService"/>.
/// </summary>
public class LibraryService : ILibraryService
{
    private readonly ILogger _logger;
    private readonly IFileHashCache _fileHashCache;

    private readonly SourceCache<IDownloadActivity, PersistedDownloadStateId> _downloadActivitySourceCache = new(x => x.PersistedStateId);

    private readonly ReadOnlyObservableCollection<IDownloadActivity> _downloadActivities;
    public ReadOnlyObservableCollection<IDownloadActivity> DownloadActivities => _downloadActivities;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(ILogger<LibraryService> logger, IFileHashCache fileHashCache)
    {
        _logger = logger;
        _fileHashCache = fileHashCache;

        _downloadActivitySourceCache.Connect().Bind(out _downloadActivities).Subscribe();
    }

    /// <inheritdoc/>
    public async Task AddDownloadAsync(
        IDownloadActivity downloadActivity,
        bool addPaused = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding download `{Title}` to the library", downloadActivity.Title);

        _downloadActivitySourceCache.AddOrUpdate(downloadActivity);

        if (addPaused) return;
        await downloadActivity.Downloader.StartAsync(downloadActivity, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Optional<LocalFile.ReadOnly>> AddLocalFileAsync(AbsolutePath absolutePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding local file at `{Path}` to the library", absolutePath);

        if (!absolutePath.FileExists)
        {
            if (absolutePath.DirectoryExists())
            {
                _logger.LogError("File at `{Path}` can't be added to the library because it's a directory", absolutePath);
            }

            _logger.LogError("File at `{Path}` can't be added to the library because it doesn't exist", absolutePath);
            return Task.FromResult(Optional<LocalFile.ReadOnly>.None);
        }

        throw new NotImplementedException();
    }

    private async ValueTask HashFileAsync(AbsolutePath filePath, CancellationToken cancellationToken = default)
    {
        var hashedEntry = await _fileHashCache.IndexFileAsync(filePath, token: cancellationToken);

    }
}
