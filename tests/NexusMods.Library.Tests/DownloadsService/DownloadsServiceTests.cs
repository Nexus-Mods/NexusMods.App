using System.Reactive.Disposables;
using DynamicData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Library.Tests.DownloadsService.Helpers;
using NexusMods.Paths;
using Xunit;
using SyncHelpers = NexusMods.Library.Tests.DownloadsService.Helpers.SynchronizationHelpers;

namespace NexusMods.Library.Tests.DownloadsService;

public class DownloadsServiceTests(
    IJobMonitor jobMonitor,
    Library.DownloadsService service,
    IServiceProvider serviceProvider) : IDisposable
{
    private readonly DownloadJobFactory _jobFactory = new(jobMonitor, serviceProvider);

    // Helper methods
    private static AbsolutePath CreateTestPath(string path)
    {
        var relativePath = path.StartsWith('/') ? path.Substring(1) : path;
        return FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine(relativePath);
    }
    
    private CompositeDisposable SetupCollectionSubscriptions(
        out List<DownloadInfo> allDownloads,
        out List<DownloadInfo> completedDownloads,
        out List<DownloadInfo> activeDownloads,
        out List<DownloadInfo>? gameDownloads,
        GameId? gameId = null)
    {
        var disposables = new CompositeDisposable();
        
        // Create local lists first
        var localAllDownloads = new List<DownloadInfo>();
        var localCompletedDownloads = new List<DownloadInfo>();
        var localActiveDownloads = new List<DownloadInfo>();
        var localGameDownloads = gameId.HasValue ? new List<DownloadInfo>() : null;
        
        service.AllDownloads.Subscribe(changes => 
        {
            foreach (var change in changes)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                        localAllDownloads.Add(change.Current);
                        break;
                    case ChangeReason.Remove:
                        localAllDownloads.RemoveAll(d => d.Id == change.Key);
                        break;
                }
            }
        }).DisposeWith(disposables);
        
        service.CompletedDownloads.Subscribe(changes => 
        {
            foreach (var change in changes)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                        localCompletedDownloads.Add(change.Current);
                        break;
                    case ChangeReason.Remove:
                        localCompletedDownloads.RemoveAll(d => d.Id == change.Key);
                        break;
                }
            }
        }).DisposeWith(disposables);
        
        service.ActiveDownloads.Subscribe(changes => 
        {
            foreach (var change in changes)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                        localActiveDownloads.Add(change.Current);
                        break;
                    case ChangeReason.Remove:
                        localActiveDownloads.RemoveAll(d => d.Id == change.Key);
                        break;
                }
            }
        }).DisposeWith(disposables);
        
        if (gameId.HasValue && localGameDownloads != null)
        {
            service.GetDownloadsForGame(gameId.Value).Subscribe(changes => 
            {
                foreach (var change in changes)
                {
                    switch (change.Reason)
                    {
                        case ChangeReason.Add:
                        case ChangeReason.Update:
                            localGameDownloads.Add(change.Current);
                            break;
                        case ChangeReason.Remove:
                            localGameDownloads.RemoveAll(d => d.Id == change.Key);
                            break;
                    }
                }
            }).DisposeWith(disposables);
        }
        
        // Assign to out parameters
        allDownloads = localAllDownloads;
        completedDownloads = localCompletedDownloads;
        activeDownloads = localActiveDownloads;
        gameDownloads = localGameDownloads;
        
        return disposables;
    }
    
    private CompositeDisposable SetupCollectionSubscriptions(
        out List<DownloadInfo> allDownloads,
        out List<DownloadInfo> completedDownloads,
        out List<DownloadInfo> activeDownloads)
    {
        return SetupCollectionSubscriptions(out allDownloads, out completedDownloads, out activeDownloads, out _, null);
    }
    
    [Fact]
    public async Task Validate_Download_Jobs_Lifetime()
    {
        // Arrange
        var gameId = GameId.From(1234u);
        var fileName = "TestMod.zip";
        var downloadUri = new Uri("https://example.com/test.zip");
        var destination = CreateTestPath("/test/downloads/TestMod.zip");
        
        // Subscribe to collections - SourceCache publishes immediately on subscribe
        using var disposables = SetupCollectionSubscriptions(
            out var allDownloads,
            out var completedDownloads,
            out var activeDownloads,
            out var gameDownloads,
            gameId);

        // Act & Assert
        
        // 1. No jobs initially
        allDownloads.Should().BeEmpty("no jobs should exist initially");
        completedDownloads.Should().BeEmpty("no completed jobs should exist initially");
        activeDownloads.Should().BeEmpty("no active jobs should exist initially");
        gameDownloads!.Should().BeEmpty("no game-specific jobs should exist initially");
        
        // 2. Start job with signals for proper synchronization
        var context = _jobFactory.CreateAndStartDownloadJob(
            fileName, gameId, downloadUri, destination, useSignals: true);
        
        // Wait for jobs to signal they're ready before checking state
        context.WaitForJobsReady(TimeSpan.FromSeconds(30))
            .Should().BeTrue("jobs should signal ready within timeout");
        
        // Signal jobs to start and wait for collections to be updated
        context.SignalJobsToStart();
        
        // Wait for job to appear in collections with proper timeout
        SyncHelpers.WaitForCollectionCount(allDownloads, 1, TimeSpan.FromSeconds(30))
            .Should().BeTrue("job should be in AllDownloads when started");
        SyncHelpers.WaitForCollectionCount(activeDownloads, 1, TimeSpan.FromSeconds(30))
            .Should().BeTrue("job should be in ActiveDownloads when started");
        SyncHelpers.WaitForCollectionCount(gameDownloads!, 1, TimeSpan.FromSeconds(30))
            .Should().BeTrue("job should be in game-specific downloads when started");
        completedDownloads.Should().BeEmpty("job should not be in CompletedDownloads when started");
        
        // 3. Complete job - should move to CompletedDownloads only
        context.CompleteJob();
        await context.JobTask.Job.WaitAsync();
        
        // Wait for completion to be processed by collections
        SyncHelpers.WaitForCollectionCount(completedDownloads, 1, TimeSpan.FromSeconds(30))
            .Should().BeTrue("completed job should be in CompletedDownloads");
        SyncHelpers.WaitForCollectionCount(activeDownloads, 0, TimeSpan.FromSeconds(30))
            .Should().BeTrue("completed job should not be in ActiveDownloads");
        
        // Verify final state
        allDownloads.Should().HaveCount(1, "completed job should remain in AllDownloads");
        completedDownloads.Should().HaveCount(1, "completed job should be in CompletedDownloads");
        activeDownloads.Should().BeEmpty("completed job should not be in ActiveDownloads");
        gameDownloads.Should().HaveCount(1, "completed job should remain in game-specific downloads");
    }
    
    [Fact]
    public async Task CancelledJobs_ShouldBeCompletelyRemoved_FromAllCollections()
    {
        // Arrange
        var gameId = GameId.From(1234u);
        var fileName = "TestMod.zip";
        var downloadUri = new Uri("https://example.com/test.zip");
        var destination = CreateTestPath("/test/downloads/TestMod.zip");
        
        using var disposables = SetupCollectionSubscriptions(
            out var allDownloads,
            out var completedDownloads,
            out var activeDownloads);

        // Act & Assert
        
        // Initially empty
        allDownloads.Should().BeEmpty("no jobs should exist initially");
        
        // Create and start job
        var context = _jobFactory.CreateAndStartDownloadJob(
            fileName, gameId, downloadUri, destination, useSignals: false);
        
        // Job should appear in collections
        allDownloads.Should().HaveCount(1, "job should be in AllDownloads when started");
        activeDownloads.Should().HaveCount(1, "job should be in ActiveDownloads when started");
        
        // Cancel the job
        context.CancelJob();
        
        try
        {
            await context.JobTask.Job.WaitAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Cancelled jobs should be completely removed
        allDownloads.Should().BeEmpty("cancelled jobs should be removed from AllDownloads");
        completedDownloads.Should().BeEmpty("cancelled jobs should not be in CompletedDownloads");
        activeDownloads.Should().BeEmpty("cancelled jobs should not be in ActiveDownloads");
    }
    
    [Fact]
    public async Task Collections_ShouldFilterCorrectly_BasedOnJobStatus()
    {
        // Arrange
        var gameId = GameId.From(1234u);
        var downloadUri = new Uri("https://example.com/test.zip");
        
        using var disposables = SetupCollectionSubscriptions(
            out var allDownloads,
            out var completedDownloads,
            out var activeDownloads);

        // Act & Assert
        
        // Create 3 jobs with different statuses
        var runningContext = _jobFactory.CreateAndStartDownloadJob(
            "Running.zip", gameId, downloadUri, 
            CreateTestPath("/test/Running.zip"), useSignals: false);
        
        var pausedContext = _jobFactory.CreateAndStartDownloadJob(
            "Paused.zip", gameId, downloadUri, 
            CreateTestPath("/test/Paused.zip"), JobStatus.Paused, useSignals: false);
        
        var completedContext = _jobFactory.CreateAndStartDownloadJob(
            "Completed.zip", gameId, downloadUri, 
            CreateTestPath("/test/Completed.zip"), useSignals: false);
        
        // All jobs should be in AllDownloads and ActiveDownloads initially
        allDownloads.Should().HaveCount(3, "all jobs should be in AllDownloads");
        activeDownloads.Should().HaveCount(3, "all jobs should be in ActiveDownloads initially");
        completedDownloads.Should().BeEmpty("no jobs should be completed initially");
        
        // Complete one job
        completedContext.CompleteJob();
        await completedContext.JobTask.Job.WaitAsync();

        // Assert final state
        allDownloads.Should().HaveCount(3, "all jobs should remain in AllDownloads");
        completedDownloads.Should().HaveCount(1, "only completed job should be in CompletedDownloads");
        activeDownloads.Should().HaveCount(2, "running and paused jobs should remain in ActiveDownloads");
        
        // Verify specific jobs are in correct collections
        completedDownloads.Should().Contain(d => d.Name == "Completed.zip");
        activeDownloads.Should().Contain(d => d.Name == "Running.zip");
        activeDownloads.Should().Contain(d => d.Name == "Paused.zip");
    }
    
    [Fact]
    public void GetDownloadsForGame_ShouldFilterByGameId()
    {
        // Arrange
        var gameId1 = GameId.From(1234u);
        var gameId2 = GameId.From(5678u);
        var downloadUri = new Uri("https://example.com/test.zip");
        
        using var disposables = SetupCollectionSubscriptions(
            out var allDownloads,
            out var completedDownloads,
            out var activeDownloads,
            out var game1Downloads,
            gameId1);

        // Act & Assert
        
        // Create 3 jobs: 2 for gameId1, 1 for gameId2
        var context1a = _jobFactory.CreateAndStartDownloadJob(
            "Game1_ModA.zip", gameId1, downloadUri, 
            CreateTestPath("/test/Game1_ModA.zip"), useSignals: false);
        
        var context1b = _jobFactory.CreateAndStartDownloadJob(
            "Game1_ModB.zip", gameId1, downloadUri, 
            CreateTestPath("/test/Game1_ModB.zip"), useSignals: false);
        
        var context2 = _jobFactory.CreateAndStartDownloadJob(
            "Game2_Mod.zip", gameId2, downloadUri, 
            CreateTestPath("/test/Game2_Mod.zip"), useSignals: false);

        // All jobs should be in AllDownloads
        allDownloads.Should().HaveCount(3, "all 3 jobs should be in AllDownloads");
        
        // Only gameId1 jobs should be in game-specific collection
        game1Downloads!.Should().HaveCount(2, "should return only jobs for gameId1");
        game1Downloads.Should().Contain(d => d.Name == "Game1_ModA.zip");
        game1Downloads.Should().Contain(d => d.Name == "Game1_ModB.zip");
        game1Downloads.Should().NotContain(d => d.Name == "Game2_Mod.zip");
    }
    
    [Fact]
    public async Task Subscriptions_ShouldBeProperlyDisposed_OnJobRemoval()
    {
        // Arrange
        var gameId = GameId.From(1234u);
        var fileName = "TestMod.zip";
        var downloadUri = new Uri("https://example.com/test.zip");
        var destination = CreateTestPath("/test/downloads/TestMod.zip");
        
        using var disposables = SetupCollectionSubscriptions(
            out var allDownloads,
            out var completedDownloads,
            out var activeDownloads);

        // Act & Assert
        
        // Create and start job
        var context = _jobFactory.CreateAndStartDownloadJob(
            fileName, gameId, downloadUri, destination, useSignals: false);
        
        // Job should be registered
        allDownloads.Should().HaveCount(1, "job should be in AllDownloads");
        var downloadInfo = allDownloads.First();
        downloadInfo.Subscriptions.Should().NotBeNull("job should have active subscriptions");
        
        // Track disposal
        var wasDisposed = false;
        downloadInfo.Subscriptions!.Add(Disposable.Create(() => wasDisposed = true));
        
        // Cancel and remove job
        context.CancelJob();
        
        try
        {
            await context.JobTask.Job.WaitAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Subscriptions should be disposed and job removed
        wasDisposed.Should().BeTrue("subscriptions should be disposed when job is removed");
        allDownloads.Should().BeEmpty("cancelled job should be removed from AllDownloads");
        
        // Test service disposal - no new jobs should be processed
        service.Dispose();
        
        var newContext = _jobFactory.CreateAndStartDownloadJob(
            "NewJob.zip", gameId, downloadUri, 
            CreateTestPath("/test/NewJob.zip"), useSignals: false);
        
        // Give a brief moment for any processing (but it shouldn't process)
        Thread.Sleep(100);
        
        // Should still be empty since service was disposed
        allDownloads.Should().BeEmpty("no changes should be processed after service disposal");
    }
    
    public void Dispose()
    {
        service.Dispose();
    }
    
    // Nested Startup class for Xunit.DependencyInjection
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Reuse the main Startup configuration
            var mainStartup = new DownloadsService.Startup();
            mainStartup.ConfigureServices(services);
        }
    }
}
