using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library.Tests.DownloadsService.Helpers;

/// <summary>
/// Factory for creating test download jobs
/// </summary>
public class DownloadJobFactory(IJobMonitor jobMonitor, IServiceProvider serviceProvider)
{
    /// <summary>
    /// Creates a controllable NexusMods download job and starts it in the <see cref="IJobMonitor"/>
    /// </summary>
    public TestDownloadJobContext CreateAndStartDownloadJob(GameId gameId)
    {
        // Create control subjects
        var statusController = new BehaviorSubject<JobStatus>(JobStatus.Running);
        var progressController = new BehaviorSubject<double>(0.0);
        var completionSource = new TaskCompletionSource<AbsolutePath>();
        var httpCompletionSource = new TaskCompletionSource<AbsolutePath>();
        
        // Create synchronization signals to prevent race conditions and allow testing things like cancellation
        var startSignal = new ManualResetEventSlim(); // Job should be started
        var readySignal = new ManualResetEventSlim(); // Job is ready to start.

        // Get required services
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<TestHttpDownloadJob>>();
        
        // Create test HTTP download job
        var testHttpJob = new TestHttpDownloadJob
        {
            Client = httpClient,
            Logger = logger,
            Uri = new Uri("https://test.example/file.zip"),
            DownloadPageUri = new Uri("https://test.example/file.zip"),
            Destination = FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine("test/downloads/TestFile.zip"),
            CompletionSource = httpCompletionSource,
            StartSignal = startSignal,
            ReadySignal = readySignal
        };
        
        // Create test Nexus Mods download job
        var testNexusJob = new TestNexusModsDownloadJob
        {
            FileMetadata = CreateTestFileMetadata("TestFile.zip", gameId),
            StatusController = statusController,
            ProgressController = progressController,
            CompletionSource = completionSource,
            StartSignal = startSignal,
            ReadySignal = readySignal,
        };
        
        // Start the HTTP job first
        var httpJobTask = jobMonitor.Begin<TestHttpDownloadJob, AbsolutePath>(testHttpJob);
        testNexusJob.HttpDownloadJob = httpJobTask;
        
        // Start the job in JobMonitor - this creates a real <see cref="IJob"/>
        var jobTask = jobMonitor.Begin<TestNexusModsDownloadJob, AbsolutePath>(testNexusJob);
        
        return new TestDownloadJobContext
        {
            NexusJob = testNexusJob,
            HttpJob = testHttpJob,
            JobTask = jobTask,
            HttpJobTask = httpJobTask,
            JobMonitor = jobMonitor,
            StatusController = statusController,
            ProgressController = progressController,
            CompletionSource = completionSource,
            HttpCompletionSource = httpCompletionSource,
            StartSignal = startSignal,
            ReadySignal = readySignal,
        };
    }
    
    private NexusModsFileMetadata.ReadOnly CreateTestFileMetadata(string fileName, GameId gameId)
    {
        // Get database connection from service provider
        var connection = serviceProvider.GetRequiredService<IConnection>();
        
        // Create proper test metadata with realistic data
        using var tx = connection.BeginTransaction();
        
        // Generate deterministic test values based on filename hash for consistency
        var fileNameHash = (ulong)Math.Abs(fileName.GetHashCode());
        var gameIdHash = (ulong)Math.Abs(gameId.Value.GetHashCode());
        var uniqueId = fileNameHash + gameIdHash;
        
        var metadata = new NexusModsFileMetadata.New(tx)
        {
            Name = fileName,
            Version = "1.0.0-test",
            Size = Size.FromLong(1024 * 1024), // 1MB test file
            UploadedAt = DateTimeOffset.UtcNow.AddDays(-1), // Uploaded yesterday
            Uid = new UidForFile(FileId.From(0), gameId),
            ModPageId = NexusModsModPageMetadataId.From(uniqueId + 1000) // Related mod page ID
        };
        
        var result = tx.Commit().Result;
        return result.Remap(metadata);
    }
}

/// <summary>
/// Context object containing all controls for a test download job
/// </summary>
public class TestDownloadJobContext
{
    public required TestNexusModsDownloadJob NexusJob { get; init; }
    public required TestHttpDownloadJob HttpJob { get; init; }
    public required IJobTask<TestNexusModsDownloadJob, AbsolutePath> JobTask { get; init; }
    public required IJobTask<TestHttpDownloadJob, AbsolutePath> HttpJobTask { get; init; }
    public required IJobMonitor JobMonitor { get; init; }
    public required BehaviorSubject<JobStatus> StatusController { get; init; }
    public required BehaviorSubject<double> ProgressController { get; init; }
    public required TaskCompletionSource<AbsolutePath> CompletionSource { get; init; }
    public required TaskCompletionSource<AbsolutePath> HttpCompletionSource { get; init; }
    
    // Synchronization signals for deterministic testing
    public ManualResetEventSlim? ReadySignal { get; init; }
    public ManualResetEventSlim? StartSignal { get; init; }

    /// <summary>
    /// Simulates job completion
    /// </summary>
    public void CompleteJob()
    {
        StatusController.OnNext(JobStatus.Completed);
        ProgressController.OnNext(1.0);
        CompletionSource.TrySetResult(NexusJob.Destination);
        HttpCompletionSource.TrySetResult(HttpJob.Destination);
    }
    
    /// <summary>
    /// Simulates job cancellation
    /// </summary>
    public void CancelJob()
    {
        JobMonitor.Cancel(JobTask);
        JobMonitor.Cancel(HttpJobTask);
    }

    /// <summary>
    /// Waits for jobs to signal they are ready
    /// </summary>
    public bool WaitForJobsReady(TimeSpan timeout)
    {
        var allReady = true;
        if (ReadySignal != null)
            allReady &= ReadySignal.Wait(timeout);
        return allReady;
    }
    
    /// <summary>
    /// Signals jobs to start
    /// </summary>
    public void SignalJobsToStart() => StartSignal?.Set();
}
