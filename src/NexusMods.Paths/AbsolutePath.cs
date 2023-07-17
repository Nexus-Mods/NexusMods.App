using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.Paths.Extensions;
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
    public AbsolutePath Parent
    {
        get
        {
            var directory = PathHelpers.GetDirectoryName(Directory, FileSystem.OS);
            var fileName = PathHelpers.GetFileName(Directory, FileSystem.OS);
            return new AbsolutePath(directory.ToString(), fileName.ToString(), FileSystem);
        }
    }

    private AbsolutePath(string directory, string fileName, IFileSystem fileSystem)
    {
        Directory = directory;
        FileName = fileName;
        FileSystem = fileSystem;
    }

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from a sanitized full path.
    /// </summary>
    /// <seealso cref="FromUnsanitizedFullPath"/>
    internal static AbsolutePath FromSanitizedFullPath(ReadOnlySpan<char> fullPath, IFileSystem fileSystem)
    {
        var directory = PathHelpers.GetDirectoryName(fullPath, fileSystem.OS);
        var fileName = PathHelpers.GetFileName(fullPath, fileSystem.OS);
        return new AbsolutePath(directory.ToString(), fileName.ToString(), fileSystem);
    }

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from an unsanitized full path.
    /// </summary>
    /// <seealso cref="FromSanitizedFullPath"/>
    /// <seealso cref="FromUnsanitizedDirectoryAndFileName"/>
    internal static AbsolutePath FromUnsanitizedFullPath(ReadOnlySpan<char> fullPath, IFileSystem fileSystem)
    {
        var sanitizedPath = PathHelpers.Sanitize(fullPath, fileSystem.OS);
        return FromSanitizedFullPath(sanitizedPath, fileSystem);
    }

    /// <summary>
    /// Creates a new <see cref="AbsolutePath"/> from an unsanitized directory and file name.
    /// </summary>
    /// <seealso cref="FromUnsanitizedFullPath"/>
    internal static AbsolutePath FromUnsanitizedDirectoryAndFileName(
        string directory,
        string fileName,
        IFileSystem fileSystem)
    {
        var sanitizedDirectory = PathHelpers.Sanitize(directory, fileSystem.OS);
        var sanitizedFileName = PathHelpers.Sanitize(fileName, fileSystem.OS);
        var fullPath = PathHelpers.JoinParts(sanitizedDirectory, sanitizedFileName, fileSystem.OS);
        return FromSanitizedFullPath(fullPath, fileSystem);
    }

    /// <summary>
    /// Returns the full path with directory separators matching the passed OS.
    /// </summary>
    public string ToNativeSeparators(IOSInformation os)
    {
        return PathHelpers.ToNativeSeparators(GetFullPath(), os);
    }

    /// <summary>
    /// Returns the file name of the specified path string without the extension.
    /// </summary>
    public string GetFileNameWithoutExtension()
    {
        if (FileName.Length == 0) return string.Empty;
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
    {
        return new AbsolutePath(Directory, FileName + ext, FileSystem);
    }

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
    {
        return new AbsolutePath(Directory, PathHelpers.ReplaceExtension(FileName, ext.ToString()), FileSystem);
    }

    /// <summary>
    /// Returns the full path of the combined string.
    /// </summary>
    /// <returns>The full combined path.</returns>
    public string GetFullPath() => PathHelpers.JoinParts(Directory, FileName, FileSystem.OS);

    /// <summary>
    /// Copies the full path into <paramref name="buffer"/>.
    /// </summary>
    public void GetFullPath(Span<char> buffer)
    {
        PathHelpers.JoinParts(buffer, Directory, FileName, FileSystem.OS);
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
        var slice = PathHelpers.GetRootPart(Directory, FileSystem.OS);
        return new AbsolutePath(slice.ToString(), string.Empty, FileSystem);
    }

    /// <summary>
    /// Combines the current path with a relative path.
    /// </summary>
    public AbsolutePath Combine(RelativePath path)
    {
        var res = PathHelpers.JoinParts(GetFullPath(), path.Path, FileSystem.OS);
        return FromSanitizedFullPath(res, FileSystem);
    }

    [Obsolete(message: "This will be removed once dependents have updated.", error: true)]
    public AbsolutePath CombineUnchecked(RelativePath path) => Combine(path);

    /// <summary>
    /// Gets a path relative to another absolute path.
    /// </summary>
    /// <param name="other">The path from which the relative path should be made.</param>
    public RelativePath RelativeTo(AbsolutePath other)
    {
        var childLength = GetFullPathLength();
        var child = childLength <= 512 ? stackalloc char[childLength] : GC.AllocateUninitializedArray<char>(childLength);
        GetFullPath(child);

        var parentLength = other.GetFullPathLength();
        var parent = parentLength <= 512 ? stackalloc char[parentLength] : GC.AllocateUninitializedArray<char>(parentLength);
        other.GetFullPath(parent);

        var res = PathHelpers.RelativeTo(child, parent, FileSystem.OS);
        if (!res.IsEmpty) return new RelativePath(res.ToString());

        ThrowHelpers.PathException("Can't create path relative to paths that aren't in the same folder");
        return default;
    }

    /// <summary>
    /// Returns true if this path is a child of the specified path.
    /// </summary>
    /// <param name="parent">The path to verify.</param>
    /// <returns>True if this is a child path of the parent path; else false.</returns>
    public bool InFolder(AbsolutePath parent)
    {
        var parentLength = parent.GetFullPathLength();
        var parentSpan = parentLength <= 512 ? stackalloc char[parentLength] : GC.AllocateUninitializedArray<char>(parentLength);
        parent.GetFullPath(parentSpan);

        // NOTE(erri120):
        // We need the full path of the "parent", but only the directory name
        // of the "child".
        return PathHelpers.InFolder(Directory, parentSpan, FileSystem.OS);
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
    }
    #endregion
}
