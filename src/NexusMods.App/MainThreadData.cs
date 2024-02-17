using System.Collections.Concurrent;

namespace NexusMods.App;

/// <summary>
/// Holds global information about the application.
/// </summary>
public static class MainThreadData
{

    private static Thread? _mainThread = null!;

    /// <summary>
    /// Flags the current thread as the one that started the application.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetMainThread()
    {
        if (_mainThread == null)
            _mainThread = Thread.CurrentThread;
        else
            throw new InvalidOperationException("Can only set the main thread once.");
    }

    /// <summary>
    /// True if the current thread is the one that started the application.
    /// </summary>
    public static bool IsStartingThread => _mainThread == Thread.CurrentThread;

    /// <summary>
    /// Used to queue actions to be executed on the main thread.
    /// </summary>
    public static ConcurrentQueue<Func<int>> MainThreadActions { get; } = new();
}
