namespace NexusMods.Common;

/// <summary>
/// Extension methods tied to nullable classes.
/// </summary>
public static class NullableExtensions
{
    /// <summary>
    /// Disposes an item if it is not null asynchronously.
    /// </summary>
    /// <param name="item">The item to dispose.</param>
    /// <typeparam name="T">A disposable item type.</typeparam>
    public static async ValueTask DisposeIfNotNullAsync<T>(this T? item) where T : struct, IAsyncDisposable
    {
        if (item.HasValue)
            await item.Value.DisposeAsync();   
    }
    
    /// <summary>
    /// Disposes an item if it is not null.
    /// </summary>
    /// <param name="item">The item to dispose.</param>
    /// <typeparam name="T">A disposable item type.</typeparam>
    public static void DisposeIfNotNull<T>(this T? item) where T : struct, IDisposable
    {
        item?.Dispose();
    }
}