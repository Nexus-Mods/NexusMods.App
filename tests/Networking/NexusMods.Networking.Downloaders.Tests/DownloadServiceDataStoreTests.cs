using FluentAssertions;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tests;

public class DownloadServiceDataStoreTests(
    DownloadService downloadService,
    IServiceProvider serviceProvider,
    LocalHttpServer server,
    TemporaryFileManager temporaryFileManager,
    IHttpDownloader httpDownloader)
    : AGameTest<SkyrimSpecialEdition>(serviceProvider)
{

    // Create a new instance of the DownloadService

    [Fact]
    public async Task WhenComplete_StaysPersistedInDataStore()
    {
        var currentCount = GetTaskCountIncludingCompleted();
        await downloadService.AddTask(new Uri($"{server.Prefix}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip"));
        var newCount = GetTaskCountIncludingCompleted();
        newCount.Should().Be(currentCount + 1);
    }

    
    [Fact]
    public async Task WhenStarted_IsPersistedInDataStore()
    {
        // Should be persisted into datastore on start, because
        var currentCount = GetTasks().Count();

        var url = new Uri($"{server.Prefix}Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip");
        await downloadService.AddTask(url);

        var newCount = GetTasks().Count();
        newCount.Should().Be(currentCount + 1);
    }

    private IEnumerable<IDownloadTask> GetTasks()
    {
        return downloadService.Downloads
            .ToList();
    }


    private int GetTaskCountIncludingCompleted()
    {
        return downloadService.Downloads.Count;
    }
}
