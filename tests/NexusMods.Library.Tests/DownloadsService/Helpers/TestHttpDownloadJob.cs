using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.HttpDownloads;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Library;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Library.Tests.DownloadsService.Helpers;

/// <summary>
/// Test implementation of <see cref="IHttpDownloadState"/> for testing
/// </summary>
internal sealed class TestHttpDownloadState : ReactiveObject, IHttpDownloadState
{
    [Reactive] public Optional<Size> ContentLength { get; set; } = Optional<Size>.None;
    [Reactive] public Size TotalBytesDownloaded { get; set; } = Size.Zero;
}

/// <summary>
/// Test implementation of <see cref="IHttpDownloadJob"/> for testing
/// </summary>
public record TestHttpDownloadJob : IJobDefinitionWithStart<TestHttpDownloadJob, AbsolutePath>, IHttpDownloadJob
{
    public required HttpClient Client { get; init; }
    public required ILogger Logger { get; init; }
    public required Uri Uri { get; init; }
    public required Uri DownloadPageUri { get; init; }
    public required AbsolutePath Destination { get; init; }
    public required TaskCompletionSource<AbsolutePath> CompletionSource { get; init; }
    
    // Synchronization signals for deterministic testing
    public ManualResetEventSlim? StartSignal { get; init; }
    public ManualResetEventSlim? ReadySignal { get; init; }
    
    private readonly TestHttpDownloadState _state = new();
    
    public bool SupportsPausing => true;
    
    public IPublicJobStateData? GetJobStateData() => _state;
    
    public async ValueTask<AbsolutePath> StartAsync(IJobContext<TestHttpDownloadJob> context)
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
        
        // Wait for completion signal from test
        return await CompletionSource.Task;
    }
    
    public ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile)
    {
        // For testing purposes, we don't need to add any metadata
        return ValueTask.CompletedTask;
    }
}
