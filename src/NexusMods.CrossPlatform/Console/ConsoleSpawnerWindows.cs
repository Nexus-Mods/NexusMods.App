using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace NexusMods.CrossPlatform.Console;

/// <remarks>
///     Spawns a console on Windows.
///     Stripped down version of https://github.com/Reloaded-Project/Reloaded-II/blob/96924a5a05e210427aaacea066a608a14005ef50/source/Reloaded.Mod.Loader/Logging/Init/ConsoleAllocator.cs
///
///     I added setting the Std handle here, games normally don't need it, but we do.
/// </remarks>
public class ConsoleSpawnerWindows : IConsoleSpawner
{
    /// <inheritdoc />
    public bool Spawn() => Alloc();

    /// <summary>
    /// Returns true if a console window is present for this process, else false.
    /// </summary>
    private static bool ConsoleExists => GetConsoleWindow() != IntPtr.Zero;

    /// <summary>
    /// Creates a new console instance if no console is present for the console.
    /// </summary>
    private static bool Alloc()
    {
        var result = ConsoleExists || AllocConsole();

        if (!result)
            return result;

        // We need to open the standard output stream to make Console.WriteLine work.
        // Normally games don't need this, but we do.
        var stdout = new StreamWriter(System.Console.OpenStandardOutput());
        stdout.AutoFlush = true;
        System.Console.SetOut(stdout);

        var stdin = new StreamReader(System.Console.OpenStandardInput());
        System.Console.SetIn(stdin);

        var stdErr = new StreamWriter(System.Console.OpenStandardError());
        stdErr.AutoFlush = true;
        System.Console.SetError(stdErr);
        
        return result;
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
}
