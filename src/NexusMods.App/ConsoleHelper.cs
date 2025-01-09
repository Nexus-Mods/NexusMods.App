using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace NexusMods.App;

/// <summary>
/// Helpers for consoles on Windows
/// </summary>
[SupportedOSPlatform("windows")]
public static class ConsoleHelper
{
    // ReSharper disable once InconsistentNaming
    private const int ATTACH_PARENT_PROCESS = -1;
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
    
    /// <summary>
    /// Attempt to attach to the console, if it fails, create a new console window if desired
    /// </summary>
    /// <param name="forceNewConsoleIfNoParent">If there is no parent console, should one be created?</param>
    public static void EnsureConsole(bool forceNewConsoleIfNoParent = false)
    {
        if (!AttachConsole(ATTACH_PARENT_PROCESS) && !forceNewConsoleIfNoParent)
        {
            AllocConsole();
        }
    }
}
