using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.HighPerformance.CommunityToolkit;
using NexusMods.Paths.Utilities;

[assembly: InternalsVisibleTo("NexusMods.Paths.Tests")]
namespace NexusMods.Paths;

/// <summary>
/// A path that represents a full path to a file or directory.
/// </summary>
[PublicAPI]
public readonly partial struct AbsolutePath : IEquatable<AbsolutePath>, IPath
{
    /// <summary>
    /// The directory component of the path.
    /// </summary>
    /// <remarks>
    /// This string is never empty and might end with a directory separator.
    /// This is only guaranteed for root directories, every other directory
    /// shall not have trailing directory separators.
    /// </remarks>
    /// <example><c>/foo/bar</c></example>
    public readonly string Directory;

    /// <summary>
    /// The characters after the last directory separator.
    /// </summary>
    /// <remarks>
    /// This string can be empty if the entire path is just a root directory.
    /// </remarks>
    /// <example><c>README.md</c></example>
    public readonly string FileName;

    /// <summary>
    /// The <see cref="IFileSystem"/> implementation used by the IO methods.
    /// </summary>
    public IFileSystem FileSystem { get; init; }

    /// <summary>
    /// Returns a new path, identical to this one, but with the filesystem replaced with the given filesystem
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <returns></returns>
    public AbsolutePath WithFileSystem(IFileSystem fileSystem)
    {
        return new AbsolutePath(Directory, FileName, fileSystem);
    }

    /// <inheritdoc />
    public Extension Extension => string.IsNullOrEmpty(FileName) ? Extension.None : Extension.FromPath(FileName);

    /// <inheritdoc />
    RelativePath IPath.FileName => FileName;

    /// <summary>
    /// Gets the parent directory, i.e. navigates one folder up.
    /// </summary>
    public AbsolutePath Parent => FromFullPath(Directory, FileSystem);

    private AbsolutePath(string directory, string fileName, IFileSystem fileSystem)
    {
        Directory = directory;
        FileName = fileName;

        FileSystem = fileSystem;
    }

    internal static AbsolutePath FromDirectoryAndFileName(string directoryPath, string fileName, IFileSystem fileSystem)
        => new(directoryPath, fileName, fileSystem);

    internal static AbsolutePath FromFullPath(string fullPath, IFileSystem fileSystem)
    {
        var span = fullPath.AsSpan();

        // path is not rooted
        var rootLength = PathHelpers.GetRootLength(span);
        if (rootLength == 0)
            throw new ArgumentException($"The provided path is not rooted: \"{fullPath}\"", nameof(fullPath));

        // path is only the root directory
        if (span.Length == rootLength)
            return new AbsolutePath(fullPath, "", fileSystem);

        var slice = span.SliceFast(rootLength);
        if (slice.DangerousGetReferenceAt(slice.Length - 1) == PathHelpers.DirectorySeparatorChar)
            slice = slice.SliceFast(0, slice.Length - 1);

        var separatorIndex = slice.LastIndexOf(PathHelpers.DirectorySeparatorChar);
        if (separatorIndex == -1)
        {
            // root directory (eg: "/" or "C:\\") is the directory
            return new AbsolutePath(span.SliceFast(0, rootLength).ToString(), slice.ToString(), fileSystem);
        }

        // everything before the separator
        var directorySpan = span.SliceFast(0, rootLength + separatorIndex);
        // everything after the separator (+1 since we don't want "/foo" but "foo")
        var fileNameSpan = slice.SliceFast(separatorIndex + 1);

        return new AbsolutePath(directorySpan.ToString(), fileNameSpan.ToString(), fileSystem);
    }

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from a sanitized full path.
    /// </summary>
    /// <seealso cref="FromUnsanitizedFullPath"/>
    internal static AbsolutePath FromSanitizedFullPath(ReadOnlySpan<char> fullPath, IFileSystem fileSystem, IOSInformation? os = null)
    {
        var directory = PathHelpers.GetDirectoryName(fullPath, os);
        var fileName = PathHelpers.GetFileName(fullPath, os);
        return new AbsolutePath(directory.ToString(), fileName.ToString(), fileSystem);
    }

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from an unsanitized full path.
    /// </summary>
    /// <seealso cref="FromSanitizedFullPath"/>
    internal static AbsolutePath FromUnsanitizedFullPath(ReadOnlySpan<char> fullPath, IFileSystem fileSystem, IOSInformation? os = null)
    {
        var sanitizedPath = PathHelpers.Sanitize(fullPath, os);
        return FromSanitizedFullPath(sanitizedPath, fileSystem, os);
    }

    /// <summary>
    /// Returns the file name of the specified path string without the extension.
    /// </summary>
    public string GetFileNameWithoutExtension()
    {
        var span = FileName.AsSpan();
        var length = span.LastIndexOf('.');
        return length >= 0 ? span.SliceFast(0, length).ToString() : span.ToString();
    }

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from the current one, appending the provided
    /// extension to the file name.
    /// </summary>
    /// <remarks>
    /// Do not use this method if you want to change the extension. Use <see cref="ReplaceExtension"/>
    /// instead. This method literally just does <see cref="FileName"/> + <paramref name="ext"/>.
    /// </remarks>
    /// <param name="ext">The extension to append.</param>
    public AbsolutePath AppendExtension(Extension ext)
        => FromDirectoryAndFileName(Directory, FileName + ext, FileSystem);

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from the current one, replacing
    /// the existing extension with a new one.
    /// </summary>
    /// <remarks>
    /// This method will behave the same as <see cref="AppendExtension"/>, if the
    /// current <see cref="FileName"/> doesn't have an extension.
    /// </remarks>
    /// <param name="ext">The extension to replace.</param>
    public AbsolutePath ReplaceExtension(Extension ext)
        => FromDirectoryAndFileName(Directory, PathHelpers.ReplaceExtension(FileName, ext.ToString()), FileSystem);

    /// <summary>
    /// Returns the full path of the combined string.
    /// </summary>
    /// <returns>The full combined path.</returns>
    public string GetFullPath() => PathHelpers.JoinParts(Directory, FileName);

    /// <summary>
    /// Copies the full path into <paramref name="buffer"/>.
    /// </summary>
    public void GetFullPath(Span<char> buffer)
    {
        PathHelpers.JoinParts(buffer, Directory, FileName);
    }

    /// <summary>
    /// Returns the expected length of the full path.
    /// </summary>
    /// <returns></returns>
    public int GetFullPathLength() => PathHelpers.GetExactJoinedPartLength(Directory, FileName);

    /// <summary>
    /// Obtains the name of the first folder stored in this path.
    /// </summary>
    public AbsolutePath GetRootDirectory()
    {
        var slice = PathHelpers.GetRootPart(Directory);
        return FromDirectoryAndFileName(slice.ToString(), string.Empty, FileSystem);
    }

    /// <summary>
    /// Joins the current absolute path with a relative path without checking the case sensitivity of a path.
    /// </summary>
    /// <param name="path">
    ///    The relative path.
    /// </param>
    /// <remarks>
    ///    Use this for paths created by our application; i.e. where we can
    ///    guarantee casing will match the specified relative path.
    /// </remarks>
    [SkipLocalsInit]
    public AbsolutePath CombineUnchecked(RelativePath path)
    {
        var res = PathHelpers.JoinParts(GetFullPath(), path.Path);
        return FromSanitizedFullPath(res, FileSystem);
    }

    /// <summary>
    /// Joins the current absolute path with a relative path.
    /// </summary>
    /// <param name="path">
    ///    The relative path; stored case insensitive.
    /// </param>
    [SkipLocalsInit]
    public AbsolutePath CombineChecked(RelativePath path)
    {
        if (path.Path.Length == 0)  return this;

        // Note: Do not special case this for Windows.
        // We have a constraint where Directory and FileName are normalised
        // to follow OS casing. And Equals/GetHashCode relies on this.

        // Since AbsolutePaths are created from the OS; we will assume the existing path is already correct, therefore
        // we only need to checked combine the relative path.

        // Now walk the directories.
        return FromFullPath(AppendChecked(GetFullPath(), path.Path), FileSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AppendChecked(string path, ReadOnlySpan<char> relativeSpan)
    {
        var remainingPath = relativeSpan;
        ReadOnlySpan<char> splitSpan;

        // path = "/foo"
        // relative = "bar/baz"

        // SplitDir("bar/baz") -> "bar"
        // splitSpan = "bar"
        // remainingPath = "bar/baz"
        while ((splitSpan = SplitDir(remainingPath)) != remainingPath)
        {
            // path = "/foo/bar"
            path = PathHelpers.JoinParts(path, FindFileOrDirectoryCasing(path, splitSpan));
            // remainingPath = "baz"
            remainingPath = remainingPath[(splitSpan.Length + 1)..];
        }

        if (remainingPath.Length > 0)
            path = PathHelpers.JoinParts(path, FindFileOrDirectoryCasing(path, remainingPath));

        return path;
    }

    /// <summary>
    /// Gets a path relative to another absolute path.
    /// </summary>
    /// <param name="other">The path from which the relative path should be made.</param>
    public RelativePath RelativeTo(AbsolutePath other)
    {
        var childLength = GetFullPathLength();
        var child = childLength <= 512 ? stackalloc char[childLength] : GC.AllocateUninitializedArray<char>(childLength);
        GetFullPath(child);

        var parentLength = GetFullPathLength();
        var parent = parentLength <= 512 ? stackalloc char[parentLength] : GC.AllocateUninitializedArray<char>(parentLength);
        other.GetFullPath(parent);

        var res = PathHelpers.RelativeTo(child, parent);
        if (!res.IsEmpty) return new RelativePath(res.ToString());

        ThrowHelpers.PathException("Can't create path relative to paths that aren't in the same folder");
        return default;
    }

    /// <summary>
    /// Returns true if this path is a child of the specified path.
    /// </summary>
    /// <param name="parent">The path to verify.</param>
    /// <returns>True if this is a child path of the parent path; else false.</returns>
    [SkipLocalsInit]
    public bool InFolder(AbsolutePath parent)
    {
        var parentLength = parent.GetFullPathLength();
        var parentSpan = parentLength <= 512 ? stackalloc char[parentLength] : GC.AllocateUninitializedArray<char>(parentLength);
        parent.GetFullPath(parentSpan);

        // NOTE(erri120):
        // We need the full path of the "parent", but only the directory name
        // of the "child".
        return PathHelpers.InFolder(Directory, parentSpan);
    }

    /// <summary/>
    public static bool operator ==(AbsolutePath lhs, AbsolutePath rhs) => lhs.Equals(rhs);

    /// <summary/>
    public static bool operator !=(AbsolutePath lhs, AbsolutePath rhs) => !(lhs == rhs);

    /// <inheritdoc />
    public override string ToString()
    {
        return this == default ? "<default>" : GetFullPath();
    }

    #region Equals & GetHashCode

    /// <inheritdoc />
    public bool Equals(AbsolutePath other)
    {
        return string.Equals(Directory, other.Directory, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is AbsolutePath other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var a = string.GetHashCode(Directory, StringComparison.OrdinalIgnoreCase);
        var b = string.GetHashCode(FileName, StringComparison.OrdinalIgnoreCase);
        return HashCode.Combine(a, b);
        // var a = StringExtensions.GetNonRandomizedHashCode(Directory);
        // var b = StringExtensions.GetNonRandomizedHashCode(FileName);
        // return HashCode.Combine(a, b);
    }
    #endregion

    /// <summary>
    /// Splits the string up to the next directory separator.
    /// </summary>
    /// <param name="text">The text to substring.</param>
    /// <returns>The text up to next directory separator, else unchanged.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> SplitDir(ReadOnlySpan<char> text)
    {
        var index = text.IndexOf(PathHelpers.DirectorySeparatorChar);
        return index != -1 ? text[..index] : text;
    }

    private static ReadOnlySpan<char> FindFileOrDirectoryCasing(string searchDir, ReadOnlySpan<char> fileName)
    {
        try
        {
            foreach (var entry in System.IO.Directory.EnumerateFileSystemEntries(searchDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                var entryFileName = Path.GetFileName(entry.AsSpan());
                if (fileName.Equals(entryFileName, StringComparison.OrdinalIgnoreCase))
                    return entryFileName;
            }
        }
        catch (Exception)
        {
            // Fallback if directory not found or cannot be accessed.
            // Static logger here would be nice.
            return fileName;
        }

        return fileName;
    }
}
