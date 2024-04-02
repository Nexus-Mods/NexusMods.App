using Avalonia;
using JetBrains.Annotations;

namespace Examples;

[UsedImplicitly] // Designer stuff
public class Program
{
    [STAThread]
    public static int Main() => 0;

    /// <summary>
    /// Don't Delete this method. It's used by the Avalonia Designer.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp() => NexusMods.App.Program.BuildAvaloniaApp();
}
