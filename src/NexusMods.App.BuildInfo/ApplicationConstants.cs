using System.Diagnostics;

namespace NexusMods.App.BuildInfo;

/// <summary>
/// Constants supplied during runtime.
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// The current version of the app.
    /// </summary>
    public static Version CurrentVersion
    {
        get
        {

            if (CompileConstants.InstallationMethod == InstallationMethod.Manually || CompileConstants.IsDebug)
                return Version.Parse("0.0.0.1");
            if (Process.GetCurrentProcess().MainModule?.FileVersionInfo.FileVersion is not null)
                return Version.TryParse(Process.GetCurrentProcess().MainModule!.FileVersionInfo.FileVersion!,
                    out var version)
                    ? version
                    : Version.Parse("0.0.0.0");
            return Version.Parse("0.0.0.0");
        }
    }
}
