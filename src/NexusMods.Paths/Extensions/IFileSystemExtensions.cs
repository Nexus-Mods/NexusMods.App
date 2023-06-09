namespace NexusMods.Paths.Extensions;

/// <summary>
/// Extensions for <see cref="IFileSystem"/>.
/// </summary>
public static class IFileSystemExtensions
{
    /// <summary>
    /// Expands the known folders in the input path, possible known folders are:
    /// {EntryFolder}, {CurrentDirectory}, {HomeFolder}, {MyGames}
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="inputPath"></param>
    /// <returns></returns>
    public static string ExpandKnownFoldersPath(this IFileSystem fileSystem, string inputPath)
    {
        inputPath = inputPath.Replace("{EntryFolder}", fileSystem.GetKnownPath(KnownPath.EntryDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{CurrentDirectory}", fileSystem.GetKnownPath(KnownPath.CurrentDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{HomeFolder}", fileSystem.GetKnownPath(KnownPath.HomeDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{MyGames}", fileSystem.GetKnownPath(KnownPath.MyGamesDirectory).GetFullPath(), StringComparison.OrdinalIgnoreCase);
        return Path.GetFullPath(inputPath);
    }
}
