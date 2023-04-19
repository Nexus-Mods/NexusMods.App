namespace NexusMods.Paths.Extensions;

public static class IFileSystemExtensions
{
    public static string ExpandKnownFoldersPath(this IFileSystem fileSystem, string inputPath)
    {
        inputPath = inputPath.Replace("{EntryFolder}", fileSystem.GetKnownPath(KnownPath.EntryDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{CurrentDirectory}", fileSystem.GetKnownPath(KnownPath.CurrentDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{HomeFolder}", fileSystem.GetKnownPath(KnownPath.HomeDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{MyGames}", fileSystem.GetKnownPath(KnownPath.MyGamesDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        return Path.GetFullPath(inputPath);
    }
}
