using System.Collections.ObjectModel;
using DynamicData;
using Microsoft.Extensions.Logging;
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

    private readonly SourceCache<IDownloadActivity, PersistedDownloadStateId> _downloadActivitySourceCache = new(x => x.PersistedStateId);

    private readonly ReadOnlyObservableCollection<IDownloadActivity> _downloadActivities;
    public ReadOnlyObservableCollection<IDownloadActivity> DownloadActivities => _downloadActivities;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(ILogger<LibraryService> logger)
    {
        _logger = logger;

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
    public Task<LocalFile.ReadOnly> AddLocalFileAsync(AbsolutePath absolutePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding local file at `{Path}` to the library", absolutePath);

        throw new NotImplementedException();
    }
}
