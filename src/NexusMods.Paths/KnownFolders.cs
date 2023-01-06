using System.Reflection;

namespace NexusMods.Paths;

public static class KnownFolders
{
    public static AbsolutePath EntryFolder => AppContext.BaseDirectory.ToAbsolutePath();
    public static AbsolutePath CurrentDirectory => Directory.GetCurrentDirectory().ToAbsolutePath();

    public static AbsolutePath Documents =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToAbsolutePath();

    public static AbsolutePath MyGames => Documents.Join("My Games");
    public static AbsolutePath HomeFolder => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToAbsolutePath();
}