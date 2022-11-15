using System.Reflection;

namespace NexusMods.Paths;

public static class KnownFolders
{
    public static AbsolutePath EntryFolder => AppContext.BaseDirectory.ToAbsolutePath();
    public static AbsolutePath CurrentDirectory => Directory.GetCurrentDirectory().ToAbsolutePath();
}