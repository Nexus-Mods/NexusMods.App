using System.Collections.Concurrent;
using DynamicData;
using FluentAssertions;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Library.Tests.DownloadsService.Helpers;

public static class SynchronizationHelpers
{
    /// <summary>
    /// Waits for a job to reach the expected status within the specified timeout
    /// </summary>
    public static async Task WaitForJobState(IJob job, JobStatus expectedStatus, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (job.Status != expectedStatus && DateTime.UtcNow - startTime < timeout)
            await Task.Delay(10);
        
        job.Status.Should().Be(expectedStatus, $"Job should have reached {expectedStatus} within {timeout}");
    }
    
    /// <summary>
    /// Waits for a download collection to contain the expected number of items
    /// </summary>
    public static bool WaitForCollectionCount<T>(
        IList<T> collection, 
        int expectedCount, 
        TimeSpan timeout)
    {
        var signal = new ManualResetEventSlim();
        var collectionMonitor = new object();
        
        // Set up a monitoring task
        var monitoringTask = Task.Run(() =>
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                lock (collectionMonitor)
                {
                    if (collection.Count == expectedCount)
                    {
                        signal.Set();
                        return true;
                    }
                }
                Thread.Sleep(10);
            }
            return false;
        });
        
        // Wait for either the signal or timeout
        return signal.Wait(timeout) && monitoringTask.Result;
    }
    
    /// <summary>
    /// Waits for a download collection to match a condition
    /// </summary>
    public static bool WaitForCollectionCondition<T>(
        IList<T> collection,
        Func<IList<T>, bool> condition,
        TimeSpan timeout)
    {
        var signal = new ManualResetEventSlim();
        var collectionMonitor = new object();
        
        var monitoringTask = Task.Run(() =>
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                lock (collectionMonitor)
                {
                    if (condition(collection))
                    {
                        signal.Set();
                        return true;
                    }
                }
                Thread.Sleep(10);
            }
            return false;
        });
        
        return signal.Wait(timeout) && monitoringTask.Result;
    }
    
    /// <summary>
    /// Creates a collection change notifier that signals when changes occur
    /// </summary>
    public static CollectionChangeNotifier<T> CreateCollectionChangeNotifier<T>()
    {
        return new CollectionChangeNotifier<T>();
    }
}

/// <summary>
/// Helper class for tracking collection changes and providing synchronization points
/// </summary>
public class CollectionChangeNotifier<T>
{
    private readonly ConcurrentQueue<ChangeReason> _changes = new();
    private readonly ManualResetEventSlim _changeSignal = new();
    private readonly object _lock = new();
    
    public void NotifyChange(ChangeReason reason)
    {
        _changes.Enqueue(reason);
        _changeSignal.Set();
    }
    
    public bool WaitForChange(TimeSpan timeout)
    {
        return _changeSignal.Wait(timeout);
    }
    
    public bool WaitForSpecificChange(ChangeReason expectedReason, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (_changeSignal.Wait(100))
            {
                lock (_lock)
                {
                    while (_changes.TryDequeue(out var reason))
                    {
                        if (reason == expectedReason)
                        {
                            return true;
                        }
                    }
                    _changeSignal.Reset();
                }
            }
        }
        
        return false;
    }
    
    public void Reset()
    {
        lock (_lock)
        {
            while (_changes.TryDequeue(out _)) { }
            _changeSignal.Reset();
        }
    }
}