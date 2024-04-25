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

    /*
    [Fact]
    public void OnComplete_ShouldFireCompletedObservable()
    {
        // Arrange
        var completedObservableFired = false;
        _downloadService.CompletedTasks.Subscribe(_ => { completedObservableFired = true; });

        // Act
        _downloadService.OnComplete(_dummyTask);

        // Assert
        completedObservableFired.Should().BeTrue();
    }

    [Fact]
    public void OnCancelled_ShouldFireCancelledObservable()
    {
        // Arrange
        var cancelledObservableFired = false;
        _downloadService.CancelledTasks.Subscribe(_ => { cancelledObservableFired = true; });

        // Act
        _downloadService.OnCancelled(_dummyTask);

        // Assert
        cancelledObservableFired.Should().BeTrue();
    }

    [Fact]
    public void OnPaused_ShouldFirePausedObservable()
    {
        // Arrange
        var pausedObservableFired = false;
        _downloadService.PausedTasks.Subscribe(_ => { pausedObservableFired = true; });

        // Act
        _downloadService.OnPaused(_dummyTask);

        // Assert
        pausedObservableFired.Should().BeTrue();
    }

    [Fact]
    public void OnResumed_ShouldFireResumedObservable()
    {
        // Arrange
        var resumedObservableFired = false;
        _downloadService.ResumedTasks.Subscribe(_ => { resumedObservableFired = true; });

        // Act
        _downloadService.OnResumed(_dummyTask);

        // Assert
        resumedObservableFired.Should().BeTrue();
    }

    [Fact]
    public void GetTotalProgress_Test()
    {
        // Arrange
        // Clear all current downloads
        foreach (var task in _currentDownloads.ToArray())
        {
            task.Cancel();
        }
        _dummyTask.SizeBytes = 100;
        _dummyTask.DownloadedSizeBytes = 25;
        _dummyTask.Status = DownloadTaskStatus.Downloading;

        // Act
        _downloadService.AddTaskWithoutStarting(_dummyTask);

        // Assert
        _currentDownloads.Should().ContainSingle();
        _downloadService.GetTotalProgress().Should().Be(Optional.Some(new Percent(0.25)));
    }

    [Fact]
    public void GetTotalProgress_MultipleTest()
    {
        // Arrange
        // Clear all current downloads
        foreach (var task in _currentDownloads.ToArray())
        {
            task.Cancel();
        }
        _dummyTask.SizeBytes = 100;
        _dummyTask.DownloadedSizeBytes = 60;
        _dummyTask.Status = DownloadTaskStatus.Downloading;

        var dummyTask2 = new DummyDownloadTask(_downloadService)
        {
            SizeBytes = 100,
            DownloadedSizeBytes = 0,
            Status = DownloadTaskStatus.Downloading
        };

        // Act
        _downloadService.AddTaskWithoutStarting(_dummyTask);
        _downloadService.AddTaskWithoutStarting(dummyTask2);

        // Assert
        _currentDownloads.Should().HaveCount(2);
        _downloadService.GetTotalProgress().Should().Be(Optional.Some(new Percent(0.30)));
    }
    */
}

