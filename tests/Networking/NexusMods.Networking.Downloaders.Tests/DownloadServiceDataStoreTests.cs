using FluentAssertions;
using NexusMods.Games.RedEngine;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.Downloaders.Interfaces;

namespace NexusMods.Networking.Downloaders.Tests;

public class DownloadServiceDataStoreTests : AGameTest<Cyberpunk2077>
{
    private readonly DownloadService _downloadService;
    private readonly LocalHttpServer _server;

    public DownloadServiceDataStoreTests(DownloadService downloadService,
        IServiceProvider serviceProvider,
        LocalHttpServer server) : base(serviceProvider)
    {
        _downloadService = downloadService;
        _server = server;
    }

    // Create a new instance of the DownloadService

    [Fact]
    public async Task WhenComplete_StaysPersistedInDataStore()
    {
        var currentCount = GetTaskCountIncludingCompleted();
        await _downloadService.AddTask(new Uri($"{_server.Prefix}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip"));
        var newCount = GetTaskCountIncludingCompleted();
        newCount.Should().Be(currentCount + 1);
    }

    
    [Fact]
    public async Task WhenStarted_IsPersistedInDataStore()
    {
        // Should be persisted into datastore on start, because
        var currentCount = GetTasks().Count();

        var url = new Uri($"{_server.Prefix}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip");
        await _downloadService.AddTask(url);

        var newCount = GetTasks().Count();
        newCount.Should().Be(currentCount + 1);
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
}
