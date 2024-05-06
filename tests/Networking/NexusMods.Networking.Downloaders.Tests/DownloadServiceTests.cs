using FluentAssertions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;
using Noggog;
using ReactiveUI;

namespace NexusMods.Networking.Downloaders.Tests;

public class DownloadServiceTests
{
    // For the uninitiated with xUnit: This is initialized before every test.
    private readonly DownloadService _downloadService;
    private readonly LocalHttpServer _httpServer;
    private readonly TemporaryFileManager _temporaryFileManager;
    private IReadOnlyCollection<IDownloadTask> _downloadTasks;

    public DownloadServiceTests(DownloadService downloadService, 
        LocalHttpServer httpServer, TemporaryFileManager temporaryFileManager)
    {
        _httpServer = httpServer;
        _downloadService = downloadService;
        _temporaryFileManager = temporaryFileManager;
    }

    [Fact]
    public async Task AddTask_Uri_ShouldDownload()
    {
        var id = Guid.NewGuid().ToString();
        _httpServer.SetContent(id, "Hello, World!".ToBytes());
        

        var task = await _downloadService.AddTask(new Uri($"{_httpServer.Prefix}{id}"));
        
        List<DownloadTaskStatus> statuses = new();
        
        using var _ = task
            .ObservableForProperty(t => t.PersistentState, skipInitial:false)
            .Subscribe(s => statuses.Add(s.Value.Status));
            
        
        _downloadService.Downloads
            .Select(t => t.PersistentState.Id)
            .Should()
            .Contain(task.PersistentState.Id);

        await task.StartAsync();
        
        task.PersistentState.Status.Should().Be(DownloadTaskStatus.Completed);

        statuses.Should().ContainInOrder(
            DownloadTaskStatus.Idle,
            DownloadTaskStatus.Downloading, 
            DownloadTaskStatus.Completed);
        
        task.DownloadLocation.FileExists.Should().BeTrue();
        (await task.DownloadLocation.ReadAllTextAsync()).Should().Be("Hello, World!");
        
        task.Downloaded.Value.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task CanSuspendDownloads()
    {
        var id = Guid.NewGuid().ToString();
        _httpServer.SetContent(id, "Suspended Test".ToBytes());
        

        var task = await _downloadService.AddTask(new Uri($"{_httpServer.Prefix}{id}"));
        
        List<DownloadTaskStatus> statuses = new();

        // Pause the server so the download doesn't complete immediately
        _httpServer.IsPaused = true;
        
        using var _ = task
            .ObservableForProperty(t => t.PersistentState, skipInitial:false)
            .Subscribe(s => statuses.Add(s.Value.Status));
            
        
        _downloadService.Downloads
            .Select(t => t.PersistentState.Id)
            .Should()
            .Contain(task.PersistentState.Id);

        var unused = Task.Run(async () =>
            {
                await task.StartAsync();
            }
        );

        await Task.Delay(100);
        await task.Suspend();

        await Task.Delay(100);
        task.PersistentState.Status.Should().Be(DownloadTaskStatus.Paused);

        _httpServer.IsPaused = false;

        await task.Resume();
        
        task.PersistentState.Status.Should().Be(DownloadTaskStatus.Completed);

        statuses.Should().ContainInOrder(
            DownloadTaskStatus.Idle,
            DownloadTaskStatus.Downloading, 
            DownloadTaskStatus.Paused,
            DownloadTaskStatus.Downloading,
            DownloadTaskStatus.Completed);
        
        task.DownloadLocation.FileExists.Should().BeTrue();
        (await task.DownloadLocation.ReadAllTextAsync()).Should().Be("Suspended Test");
        
        task.Downloaded.Value.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task CanCencelDownloads()
    {
        var id = Guid.NewGuid().ToString();
        _httpServer.SetContent(id, "Suspended Test".ToBytes());
        

        var task = await _downloadService.AddTask(new Uri($"{_httpServer.Prefix}{id}"));
        
        List<DownloadTaskStatus> statuses = new();

        // Pause the server so the download doesn't complete immediately
        _httpServer.IsPaused = true;
        
        using var _ = task
            .ObservableForProperty(t => t.PersistentState, skipInitial:false)
            .Subscribe(s => statuses.Add(s.Value.Status));
            
        
        _downloadService.Downloads
            .Select(t => t.PersistentState.Id)
            .Should()
            .Contain(task.PersistentState.Id);

        var unused = Task.Run(async () =>
            {
                await task.StartAsync();
            }
        );

        await Task.Delay(100);
        await task.Cancel();
        
        statuses.Should().ContainInOrder(
            DownloadTaskStatus.Idle,
            DownloadTaskStatus.Downloading, 
            DownloadTaskStatus.Cancelled);

        _httpServer.IsPaused = false;
    }
    
}

