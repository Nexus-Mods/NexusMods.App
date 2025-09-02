using System.Reactive.Subjects;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library.Tests.DownloadsService.Helpers;

/// <summary>
/// Test implementation that implements <see cref="IJobDefinitionWithStart{TJobDefinition,TResult}"/>
/// This allows us to create real jobs that work with the real <see cref="IJobMonitor"/>
/// Note: Does not implement <see cref="INexusModsDownloadJob"/> due to type constraints with <see cref="TestHttpDownloadJob"/>
/// </summary>
public record TestNexusModsDownloadJob : IJobDefinitionWithStart<TestNexusModsDownloadJob, AbsolutePath>, INexusModsDownloadJob

{
    // Configuration properties - these come from the <see cref="IHttpDownloadJob"/> and FileMetadata
    public required NexusModsFileMetadata.ReadOnly FileMetadata { get; init; }
    
    // Control properties for testing
    public required BehaviorSubject<JobStatus> StatusController { get; init; }
    public required BehaviorSubject<double> ProgressController { get; init; }
    public required TaskCompletionSource<AbsolutePath> CompletionSource { get; init; }
    
    // Synchronization signals for deterministic testing
    public ManualResetEventSlim? StartSignal { get; init; }
    public ManualResetEventSlim? ReadySignal { get; init; }
    public ManualResetEventSlim? YieldSignal { get; init; }
    
    // <see cref="IDownloadJob"/> implementation
    public AbsolutePath Destination => HttpDownloadJob.JobDefinition.Destination;
    
    // Additional properties for test control
    public IJobTask<IHttpDownloadJob, AbsolutePath> HttpDownloadJob { get; set; } = null!;
    
    // <see cref="IJobDefinitionWithStart{TJobDefinition,TResult}"/> implementation
    public async ValueTask<AbsolutePath> StartAsync(IJobContext<TestNexusModsDownloadJob> context)
    {
        // Signal that we're ready to start (for test synchronization)
        ReadySignal?.Set();
        
        // Wait for start signal if provided (allows tests to control timing)
        if (StartSignal != null)
        {
            if (!StartSignal.Wait(TimeSpan.FromSeconds(30), context.CancellationToken))
                throw new TimeoutException("StartSignal was not set within timeout period");
        }
        
        // Subscribe to status changes
        StatusController.Subscribe(status =>
        {
            // Update job status based on controller
            if (status == JobStatus.Cancelled)
                context.CancellationToken.Register(() => CompletionSource.TrySetCanceled());
            // Note: Pausing is handled through YieldAsync() calls during execution
        });
        
        // Subscribe to progress changes
        ProgressController.Subscribe(progress =>
        {
            var totalSize = Size.From(100UL); // Fixed size for testing
            var currentSize = Size.From((ulong)(progress * 100));
            context.SetPercent(currentSize, totalSize);
            
            if (progress > 0)
            {
                var rate = progress * 10; // Arbitrary rate calculation for testing
                context.SetRateOfProgress(rate);
            }
        });
        
        // Wait for completion or cancellation with controlled yielding
        try
        {
            while (!CompletionSource.Task.IsCompleted && !context.CancellationToken.IsCancellationRequested)
            {
                await context.YieldAsync();
                
                // Wait for yield signal if provided (allows tests to control execution flow)
                if (YieldSignal != null)
                {
                    if (!YieldSignal.Wait(TimeSpan.FromSeconds(30), context.CancellationToken))
                        break; // Continue if signal times out to prevent infinite waiting
                }
            }
            
            return await CompletionSource.Task;
        }
        catch (TaskCanceledException)
        {
            throw new OperationCanceledException("Job was cancelled");
        }
    }
    
    // <see cref="IDownloadJob"/> metadata implementation
    public ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile)
    {
        // For testing purposes, we don't need to add any metadata
        return ValueTask.CompletedTask;
    }
}
