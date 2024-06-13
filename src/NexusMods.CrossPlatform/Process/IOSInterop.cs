﻿using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// abstractions for functionality that has no platform independent implementation in .NET
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IOSInterop
{
    /// <summary>
    /// open a url in the default application based on the protocol
    /// </summary>
    /// <param name="url">URI to open</param>
    /// <param name="fireAndForget">Start the process but don't wait for the completion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OpenUrl(Uri url, bool fireAndForget = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the file with the registered default application.
    /// </summary>
    Task OpenFile(AbsolutePath filePath, bool fireAndForget = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the directory with the system explorer.
    /// </summary>
    Task OpenDirectory(AbsolutePath directoryPath, bool fireAndForget = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the path to the current executable
    /// </summary>
    AbsolutePath GetOwnExe();
}
