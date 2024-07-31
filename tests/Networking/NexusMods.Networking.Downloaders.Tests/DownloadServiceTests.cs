using FluentAssertions;
using NexusMods.Abstractions.Jobs;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Networking.Downloaders.Tests;

public class DownloadServiceTests
{
    // For the uninitiated with xUnit: This is initialized before every test.
    private readonly DownloadService _downloadService;
    private readonly LocalHttpServer _httpServer;
    private IReadOnlyCollection<IDownloadTask> _downloadTasks;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly HttpDownloadJobWorker _worker;

    public DownloadServiceTests(DownloadService downloadService, 
        LocalHttpServer httpServer, TemporaryFileManager temporaryFileManager,
        HttpDownloadJobWorker worker)
    {
        _httpServer = httpServer;
        _downloadService = downloadService;
        _temporaryFileManager = temporaryFileManager;
        _worker = worker;
    }

    [Fact]
    public async Task AddTask_Uri_ShouldDownload()
    {
        var id = Guid.NewGuid().ToString();
        _httpServer.SetContent(id, "Hello, World!"u8.ToArray());
        

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
        
        // File is deleted after Analyzing and repacking
        task.DownloadPath.FileExists.Should().BeFalse();
        
        task.Downloaded.Value.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task CanDownloadWithJobInterface()
    {
        var id = Guid.NewGuid().ToString();
        _httpServer.SetContent(id, "Hello, World!"u8.ToArray());

        var location = _temporaryFileManager.CreateFile();

        await using var job = await _worker.CreateJob(new Uri($"{_httpServer.Prefix}{id}"), location);
        await job.StartAsync(CancellationToken.None);

        var result = await job.WaitToFinishAsync();
        result.ResultType.Should().Be(JobResultType.Completed);
        
        result.TryGetCompleted(out var completed).Should().BeTrue();
        
        completed!.TryGetData(out AbsolutePath path).Should().BeTrue();

        path.Should().Be(location.Path);
        
        location.Path.FileExists.Should().BeTrue();
        (await location.Path.ReadAllTextAsync()).Should().Be("Hello, World!");
        

    }
    
    [Fact]
    public async Task CanSuspendDownloads()
    {
        var id = Guid.NewGuid().ToString();
        _httpServer.SetContent(id, "Suspended Test"u8.ToArray());
        

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
        
        // File is deleted after Analyzing and repacking
        task.DownloadPath.FileExists.Should().BeFalse();
        
        task.Downloaded.Value.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task CanCencelDownloads()
    {
        var id = Guid.NewGuid().ToString();
        _httpServer.SetContent(id, "Suspended Test"u8.ToArray());
        

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

