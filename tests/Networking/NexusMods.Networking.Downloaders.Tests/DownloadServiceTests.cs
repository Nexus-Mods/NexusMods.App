using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Kernel;
using FluentAssertions;
using NexusMods.Abstractions.Activities;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Networking.Downloaders.Tasks.State;

namespace NexusMods.Networking.Downloaders.Tests;

public class DownloadServiceTests
{
    // For the uninitiated with xUnit: This is initialized before every test.
    private readonly DownloadService _downloadService;
    private readonly ReadOnlyObservableCollection<IDownloadTask> _currentDownloads;
    private readonly DummyDownloadTask _dummyTask;

    public DownloadServiceTests(DownloadService downloadService)
    {
        // Create a new instance of the DownloadService
        _downloadService = downloadService;
        _downloadService.Downloads.Bind(out _currentDownloads).Subscribe();
        _dummyTask = new DummyDownloadTask(_downloadService);
    }

    [Fact]
    public void AddTask_ShouldFireStartedObservable()
    {
        // Arrange
        var startedObservableFired = false;
        _downloadService.StartedTasks.Subscribe(_ => { startedObservableFired = true; });

        // Act
        _downloadService.AddTask(_dummyTask);

        // Assert
        startedObservableFired.Should().BeTrue();
    }

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

    private class DummyDownloadTask : IDownloadTask, IHaveFileSize
    {
        public DummyDownloadTask(DownloadService service) { Owner = service; }
        public long DownloadedSizeBytes { get; internal set; } = 0;
        public long SizeBytes { get; internal set;  } = 0;

        public long CalculateThroughput() => 0;

        public IDownloadService Owner { get; set; }
        public DownloadTaskStatus Status { get; set; }
        public string FriendlyName { get; } = "";

        public Task StartAsync()
        {
            Owner.OnComplete(this);
            return Task.CompletedTask;
        }

        public void Cancel() => Owner.OnCancelled(this);
        public void Suspend() => Owner.OnPaused(this);
        public Task Resume()
        {
            Owner.OnResumed(this);
            return Task.CompletedTask;
        }

        public DownloaderState ExportState() => DownloaderState.Create(this, null!, "");

    }
}

