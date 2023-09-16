using System.Diagnostics;

namespace NexusMods.Common;

/// <summary>
/// Constants supplied during runtime.
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// The current version of the app.
    /// </summary>
    public static Version CurrentVersion => Process.GetCurrentProcess().MainModule?.FileVersionInfo.FileVersion is not null
        ? Version.TryParse(Process.GetCurrentProcess().MainModule!.FileVersionInfo.FileVersion!, out var version) ? version : Version.Parse("0.0.0.0")
        : Version.Parse("0.0.0.0");
}
