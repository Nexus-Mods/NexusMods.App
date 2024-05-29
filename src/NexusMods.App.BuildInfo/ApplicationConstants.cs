using System.Reflection;

namespace NexusMods.App.BuildInfo;

/// <summary>
/// Constants supplied during runtime.
/// </summary>
public static class ApplicationConstants
{
    static ApplicationConstants()
    {
        Version = Assembly.GetExecutingAssembly().GetName().Version!;
    }

    /// <summary>
    /// Gets the current Version.
    /// </summary>
    public static Version Version { get; }
}
