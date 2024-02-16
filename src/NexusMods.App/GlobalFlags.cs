using System.Collections.Concurrent;

namespace NexusMods.App;

/// <summary>
/// Holds global information about the application.
/// </summary>
public static class GlobalFlags
{
    /// <summary>
    /// Holds the thread that started the application.
    /// </summary>
    public static Thread StartingThread { get; internal set; } = null!;

    /// <summary>
    /// True if the current thread is the one that started the application.
    /// </summary>
    public static bool IsStartingThread => StartingThread == Thread.CurrentThread;

    /// <summary>
    /// Used to queue actions to be executed on the main thread.
    /// </summary>
    public static ConcurrentQueue<Func<int>> MainThreadActions { get; } = new();
}
