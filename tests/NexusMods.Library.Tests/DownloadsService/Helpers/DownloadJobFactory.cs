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
    public TestDownloadJobContext CreateAndStartDownloadJob(
        string fileName,
        GameId gameId,
        Uri downloadUri,
        AbsolutePath destination,
        JobStatus initialStatus = JobStatus.Running,
        bool useSignals = true)
    {
        // Create control subjects
        var statusController = new BehaviorSubject<JobStatus>(initialStatus);
        var progressController = new BehaviorSubject<double>(0.0);
        var completionSource = new TaskCompletionSource<AbsolutePath>();
        var httpCompletionSource = new TaskCompletionSource<AbsolutePath>();
        
        // Create synchronization signals if requested
        var startSignal = useSignals ? new ManualResetEventSlim() : null;
        var httpReadySignal = useSignals ? new ManualResetEventSlim() : null;
        var nexusReadySignal = useSignals ? new ManualResetEventSlim() : null;
        var nexusYieldSignal = useSignals ? new ManualResetEventSlim() : null;
        
        // Get required services
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<TestHttpDownloadJob>>();
        
        // Create test HTTP download job
        var testHttpJob = new TestHttpDownloadJob
        {
            Client = httpClient,
            Logger = logger,
            Uri = downloadUri,
            DownloadPageUri = downloadUri,
            Destination = destination,
            CompletionSource = httpCompletionSource,
            StartSignal = startSignal,
            ReadySignal = httpReadySignal
        };
        
        // Create test Nexus Mods download job
        var testNexusJob = new TestNexusModsDownloadJob
        {
            FileMetadata = CreateTestFileMetadata(fileName, gameId),
            StatusController = statusController,
            ProgressController = progressController,
            CompletionSource = completionSource,
            StartSignal = startSignal,
            ReadySignal = nexusReadySignal,
            YieldSignal = nexusYieldSignal
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
            StatusController = statusController,
            ProgressController = progressController,
            CompletionSource = completionSource,
            HttpCompletionSource = httpCompletionSource,
            StartSignal = startSignal,
            HttpReadySignal = httpReadySignal,
            NexusReadySignal = nexusReadySignal,
            NexusYieldSignal = nexusYieldSignal
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
    public required BehaviorSubject<JobStatus> StatusController { get; init; }
    public required BehaviorSubject<double> ProgressController { get; init; }
    public required TaskCompletionSource<AbsolutePath> CompletionSource { get; init; }
    public required TaskCompletionSource<AbsolutePath> HttpCompletionSource { get; init; }
    
    // Synchronization signals for deterministic testing
    public ManualResetEventSlim? StartSignal { get; init; }
    public ManualResetEventSlim? HttpReadySignal { get; init; }
    public ManualResetEventSlim? NexusReadySignal { get; init; }
    public ManualResetEventSlim? NexusYieldSignal { get; init; }
    
    /// <summary>
    /// Simulates job completion
    /// </summary>
    public void CompleteJob()
    {
        StatusController.OnNext(JobStatus.Completed);
        ProgressController.OnNext(1.0);
        CompletionSource.TrySetResult(NexusJob.Destination);
        HttpCompletionSource.TrySetResult(HttpJob.Destination);
        
        // Signal any waiting yield operations to ensure immediate completion
        NexusYieldSignal?.Set();
    }
    
    /// <summary>
    /// Simulates job cancellation
    /// </summary>
    public void CancelJob()
    {
        StatusController.OnNext(JobStatus.Cancelled);
        CompletionSource.TrySetCanceled();
        HttpCompletionSource.TrySetCanceled();
    }

    /// <summary>
    /// Waits for jobs to signal they are ready
    /// </summary>
    public bool WaitForJobsReady(TimeSpan timeout)
    {
        var allReady = true;
        if (HttpReadySignal != null)
            allReady &= HttpReadySignal.Wait(timeout);
        if (NexusReadySignal != null)
            allReady &= NexusReadySignal.Wait(timeout);
        return allReady;
    }
    
    /// <summary>
    /// Signals jobs to start
    /// </summary>
    public void SignalJobsToStart()
    {
        StartSignal?.Set();
    }
}
