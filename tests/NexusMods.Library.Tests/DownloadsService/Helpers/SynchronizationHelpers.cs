namespace NexusMods.Library.Tests.DownloadsService.Helpers;

public static class SynchronizationHelpers
{
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
}
