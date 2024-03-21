using System.Collections.Concurrent;
using System.Diagnostics;

namespace NexusMods.App;

/// <summary>
/// Holds global information about the application.
/// </summary>
public static class MainThreadData
{
    // Run in debug mode if we are in debug mode and the debugger is attached. We use preprocessor flags here as
    // some AV software may be configured to flag processes that look for debuggers as malicious. So we don't even
    // look for a debugger unless we are in debug mode.
#if DEBUG
    public static readonly bool IsDebugMode = Debugger.IsAttached;
#else
    public const bool IsDebugMode = false;
#endif
    

    private static Thread? _mainThread = null!;
    
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    
    /// <summary>
    /// A token that's used system-wide to signal that the application is shutting down.
    /// </summary>
    public static CancellationToken GlobalShutdownToken => CancellationTokenSource.Token;
    
    /// <summary>
    /// Shuts down the application.
    /// </summary>
    public static void Shutdown()
    {
        CancellationTokenSource.Cancel();
    }
    
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
