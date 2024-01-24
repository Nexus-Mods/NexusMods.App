using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tests;

public class DownloadServiceDataStoreTests : AGameTest<SkyrimSpecialEdition>
{
    // For the uninitiated with xUnit: This is initialized before every test.
    private readonly DownloadService _downloadService;
    private readonly IDataStore _store;
    private readonly LocalHttpServer _server;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IHttpDownloader _httpDownloader;
    private readonly IServiceProvider _serviceProvider;

    public DownloadServiceDataStoreTests(DownloadService downloadService, IDataStore store, IServiceProvider serviceProvider, LocalHttpServer server, TemporaryFileManager temporaryFileManager, IHttpDownloader httpDownloader) : base(serviceProvider)
    {
        // Create a new instance of the DownloadService
        _downloadService = downloadService;
        _store = store;
        _serviceProvider = serviceProvider;
        _server = server;
        _temporaryFileManager = temporaryFileManager;
        _httpDownloader = httpDownloader;
    }

    [Fact]
    public async Task WhenComplete_StaysPersistedInDataStore()
    {
        var currentCount = GetTaskCountIncludingCompleted();
        await _downloadService.AddHttpTask($"{_server.Uri}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip");
        var newCount = GetTaskCountIncludingCompleted();
        newCount.Should().Be(currentCount + 1);
    }

    [Fact]
    public void WhenStarted_IsPersistedInDataStore()
    {
        // Should be persisted into datastore on start, because
        var currentCount = GetTasks().Count();

        var url = $"{_server.Uri}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip";
        var task = new HttpDownloadTask(_serviceProvider.GetRequiredService<ILogger<HttpDownloadTask>>(), _temporaryFileManager, _serviceProvider.GetRequiredService<HttpClient>(), _httpDownloader, _downloadService);
        var makeUrl = $"{_server.Uri}{url}";
        task.Init(makeUrl);
        _downloadService.AddTaskWithoutStarting(task);

        var newCount = GetTasks().Count();
        newCount.Should().Be(currentCount + 1);
    }

    [Fact]
    public void WhenCancelled_IsRemovedFromDataStore()
    {
        // Should be persisted into datastore on start, because
        var currentCount = GetTasks().Count();

        var url = $"{_server.Uri}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip";
        var task = new HttpDownloadTask(_serviceProvider.GetRequiredService<ILogger<HttpDownloadTask>>(), _temporaryFileManager, _serviceProvider.GetRequiredService<HttpClient>(), _httpDownloader, _downloadService);
        var makeUrl = $"{_server.Uri}{url}";
        task.Init(makeUrl);
        _downloadService.AddTaskWithoutStarting(task);
        task.Cancel();

        var newCount = GetTasks().Count();
        newCount.Should().Be(currentCount);
    }

    [Fact]
    public void WhenRestarted_IsRestored()
    {
        // Should be persisted into datastore on start, because
        var url = $"{_server.Uri}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip";
        var task = new HttpDownloadTask(_serviceProvider.GetRequiredService<ILogger<HttpDownloadTask>>(), _temporaryFileManager, _serviceProvider.GetRequiredService<HttpClient>(), _httpDownloader, _downloadService);
        var makeUrl = $"{_server.Uri}{url}";
        task.Init(makeUrl);
        _downloadService.AddTaskWithoutStarting(task);

        // Suppose app crashed during download.
        // Here we check that when restarted, our task will be restored.
        _downloadService.GetItemsToResume().Count().Should().Be(1);
    }

    private IEnumerable<IDownloadTask> GetTasks()
    {
        return _store.AllIds(EntityCategory.DownloadStates)
            .Select(id => _store.Get<DownloaderState>(id))
            .Where(state => state != null)
            .Select(state => _downloadService.GetTaskFromState(state!))
            .Where(x => x != null)
            .Cast<IDownloadTask>();
    }

    private int GetTaskCountIncludingCompleted()
    {
        return _store.AllIds(EntityCategory.DownloadStates).Count();
    }
}
