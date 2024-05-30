using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.IO;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tests;

public class DownloadServiceDataStoreTests : IAsyncDisposable
{
    private readonly LocalHttpServer _server;
    private readonly DownloadService _downloadService;

    public DownloadServiceDataStoreTests(IServiceProvider serviceProvider,
        LocalHttpServer server)
    {
        _server = server;
        _downloadService = new DownloadService(serviceProvider.GetRequiredService<ILogger<DownloadService>>(),
            serviceProvider, 
            serviceProvider.GetRequiredService<IFileStore>(), 
            serviceProvider.GetRequiredService<IConnection>());
    }

    // Create a new instance of the DownloadService

    [Fact]
    public async Task WhenComplete_StaysPersistedInDataStore()
    {
        var currentCount = GetTaskCountIncludingCompleted();
        await _downloadService.AddTask(new Uri($"{_server.Prefix}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip"));
        var newCount = GetTaskCountIncludingCompleted();
        newCount.Should().BeGreaterOrEqualTo(currentCount + 1);
    }

    
    [Fact]
    public async Task WhenStarted_IsPersistedInDataStore()
    {
        // Should be persisted into datastore on start, because
        var currentCount = GetTasks().Count();

        var url = new Uri($"{_server.Prefix}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip");
        await _downloadService.AddTask(url);

        var newCount = GetTasks().Count();
        newCount.Should().BeGreaterOrEqualTo(currentCount + 1);
    }

    private IEnumerable<IDownloadTask> GetTasks()
    {
        return _downloadService.Downloads
            .ToList();
    }


    private int GetTaskCountIncludingCompleted()
    {
        return _downloadService.Downloads.Count;
    }

    public async ValueTask DisposeAsync()
    {
        _server.Dispose();
        await _downloadService.DisposeAsync();
    }
}
