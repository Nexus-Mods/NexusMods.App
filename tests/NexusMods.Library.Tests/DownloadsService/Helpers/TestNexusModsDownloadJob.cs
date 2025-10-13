using System.Reactive.Subjects;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;

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
        
        // Yield to allow other operations
        await context.YieldAsync();
            
        // Simply await the completion source - matches original NexusModsDownloadJob pattern
        return await CompletionSource.Task;
    }
    
    // <see cref="IDownloadJob"/> metadata implementation
    public ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile)
    {
        // For testing purposes, we don't need to add any metadata
        return ValueTask.CompletedTask;
    }
}
