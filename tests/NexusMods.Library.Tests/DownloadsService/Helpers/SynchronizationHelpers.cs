namespace NexusMods.Library.Tests.DownloadsService.Helpers;

public static class SynchronizationHelpers
{
    /// <summary>
    /// Waits for a download collection to contain the expected number of items
    /// </summary>
    public static async Task<bool> WaitForCollectionCount<T>(
        IList<T> collection, 
        int expectedCount, 
        TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (collection.Count == expectedCount)
            {
                return true;
            }
            
            await Task.Delay(10);
        }
        
        return false;
    }
}
