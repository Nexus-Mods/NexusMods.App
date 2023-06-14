using System.Text;

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
    public static AbsolutePath ExpandKnownFoldersPath(this IFileSystem fileSystem, string inputPath)
    {
        var sb = new StringBuilder(inputPath.Length);
        sb.Append(inputPath);
        sb.Replace("{EntryFolder}", fileSystem.GetKnownPath(KnownPath.EntryDirectory).GetFullPath());
        sb.Replace("{CurrentDirectory}", fileSystem.GetKnownPath(KnownPath.CurrentDirectory).GetFullPath());
        sb.Replace("{HomeFolder}", fileSystem.GetKnownPath(KnownPath.HomeDirectory).GetFullPath());
        sb.Replace("{MyGames}", fileSystem.GetKnownPath(KnownPath.MyGamesDirectory).GetFullPath());
        return fileSystem.FromUnsanitizedFullPath(sb.ToString());
    }
}
