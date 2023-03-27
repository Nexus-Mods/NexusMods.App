using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.Utilities;

/// <summary>
/// Contains a listing of known ahead of time folders for easy access.
/// </summary>
[Obsolete($"This class is obsolete, use IFileSystem.{nameof(IFileSystem.GetKnownPath)}")]
public static class KnownFolders
{
    /// <inheritdoc cref="KnownPath.EntryDirectory"/>
    [Obsolete($"This property is obsolete, use IFileSystem.{nameof(IFileSystem.GetKnownPath)} directly.")]
    public static AbsolutePath EntryFolder => FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory);

    /// <inheritdoc cref="KnownPath.CurrentDirectory"/>
    [Obsolete($"This property is obsolete, use IFileSystem.{nameof(IFileSystem.GetKnownPath)} directly.")]
    public static AbsolutePath CurrentDirectory => FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory);

    /// <inheritdoc cref="KnownPath.MyGamesDirectory"/>
    [Obsolete($"This property is obsolete, use IFileSystem.{nameof(IFileSystem.GetKnownPath)} directly.")]
    public static AbsolutePath MyGames => FileSystem.Shared.GetKnownPath(KnownPath.MyGamesDirectory);

    /// <inheritdoc cref="KnownPath.HomeDirectory"/>
    [Obsolete($"This property is obsolete, use IFileSystem.{nameof(IFileSystem.GetKnownPath)} directly.")]
    public static AbsolutePath HomeFolder => FileSystem.Shared.GetKnownPath(KnownPath.HomeDirectory);

    /// <summary>
    /// Accepts a given path and expands it using known monikers for various folders.
    /// </summary>
    /// <param name="inputPath">The path to expand.</param>
    /// <returns>New, expanded path.</returns>
    /// <remarks>
    ///    Not optimised, originally intended for use in configs.
    ///    Do not use in hot paths.
    /// </remarks>
    public static string ExpandPath(string inputPath)
    {
        inputPath = inputPath.Replace("{EntryFolder}", EntryFolder.GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{CurrentDirectory}", CurrentDirectory.GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{HomeFolder}", HomeFolder.GetFullPath(), StringComparison.OrdinalIgnoreCase);
        inputPath = inputPath.Replace("{MyGames}", MyGames.GetFullPath(), StringComparison.OrdinalIgnoreCase);
        return Path.GetFullPath(inputPath);
    }
}
