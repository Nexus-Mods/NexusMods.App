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
}
