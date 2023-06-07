namespace NexusMods.Paths.Tests;

/// <summary>
/// Helpers for doing common OS checks
/// </summary>
public static class OSHelper
{
    
    /// <summary>
    /// Returns true if the current operating system is Linux, OSX, or other Unix-like OSes
    /// </summary>
    /// <returns></returns>
    public static bool IsUnixLike()
    {
        return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
    }
}
