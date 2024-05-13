using System;
using NexusMods.Paths;

namespace NexusMods.SingleProcess;

/// <summary>
/// App wide settings for the single process application.
/// </summary>
public class SingleProcessSettings
{
    /// <summary>
    /// The path to the sync file, this file is used to publish the process id of the main process, and the TCP port it's listening on.
    /// </summary>
    public required AbsolutePath SyncFile { get; init; }

    /// <summary>
    /// The size of the sync file, this should be at least the width of two uints. One will be used to store the process
    /// id of the main process, the other will be used to store the TCP port the main process is listening on.
    /// </summary>
    public Size SyncFileSize { get; init; } = Size.FromLong(8);

    /// <summary>
    /// The amount of time the TCPListener will pause waiting for new connections before checking if it should exit.
    /// </summary>
    public TimeSpan ListenTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The amount of time the main process will wait for new connections before terminating.
    /// </summary>
    public TimeSpan StayRunningTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The amount of time the client process will wait for the main process to start before giving up.
    /// </summary>
    public TimeSpan ClientConnectTimeout { get; set; } = TimeSpan.FromSeconds(60);
}
