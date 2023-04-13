using System.Runtime.CompilerServices;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// A path that represents a partial path to a file or directory.
/// </summary>
public struct RelativePath : IEquatable<RelativePath>, IPath, IComparable<RelativePath>
{
    /// <summary>
    /// Gets the comparer used for sorting.
    /// </summary>
    public static readonly RelativePathComparer Comparer = new();

    /// <summary>
    /// Represents an empty path.
    /// </summary>
    public static RelativePath Empty => new(string.Empty);

    /// <summary>
    /// Contains the relative path stored in this instance.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <inheritdoc />
    public Extension Extension => Extension.FromPath(Path);

    /// <inheritdoc />
    public RelativePath FileName => PathHelpers.GetFileName(Path);

    /// <summary>
    /// Amount of directories contained within this relative path.
    /// </summary>
    public int Depth => Path.AsSpan().Count('/') + Path.AsSpan().Count('\\');
    // Note: Relative paths can be declared using either separator.

    /// <summary>
    /// Traverses one directory up.
    /// </summary>
    public RelativePath Parent => new(PathHelpers.GetDirectoryName(Path) ?? "");

    /// <summary>
    /// Obtains the name of the first folder stored in this path.
    /// </summary>
    /// <remarks>
    ///    This will return empty string if there are no child directories.
    /// </remarks>
    public RelativePath TopParent => Path[..Math.Max(GetFirstDirectorySeparatorIndex(out _), 0)];

    /// <summary>
    /// Creates a relative path given a string.
    /// </summary>
    /// <param name="path">The relative path to use.</param>
    public RelativePath(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Returns a new path that is this path with the extension changed.
    /// </summary>
    /// <param name="newExtension">The extension to replace the old extension.</param>
    public RelativePath ReplaceExtension(Extension newExtension) => new(Path.ReplaceExtension(newExtension));

    /// <summary>
    /// Adds an extension to the relative path.
    /// </summary>
    /// <param name="ext">The extension to add.</param>
    public RelativePath WithExtension(Extension ext) => new RelativePath(Path + ext);

    /// <summary>
    /// Appends another path to an existing path.
    /// </summary>
    /// <param name="other">The path to append.</param>
    /// <returns>Combinations of both paths.</returns>
    public RelativePath Join(RelativePath other)
    {
        return new RelativePath(string.Concat(Path, DetermineDirectorySeparatorString(), other.Path));
    }

    /// <summary>
    /// Returns true if the relative path starts with a given string.
    /// </summary>
    public bool StartsWith(ReadOnlySpan<char> other)
    {
        // Note: We assume equality to be separator and case insensitive
        //       therefore this property should transfer over to contains checks.
        var thisCopy = Path.Length <= 512 ? stackalloc char[Path.Length] : GC.AllocateUninitializedArray<char>(Path.Length);
        Path.CopyTo(thisCopy);
        thisCopy.NormalizeStringCaseAndPathSeparator();

        var otherCopy = other.Length <= 512 ? stackalloc char[other.Length] : GC.AllocateUninitializedArray<char>(other.Length);
        other.CopyTo(otherCopy);
        otherCopy.NormalizeStringCaseAndPathSeparator();

        return thisCopy.StartsWith(otherCopy);
    }

    /// <summary>
    /// Returns true if this path is a child of this path.
    /// </summary>
    /// <param name="other">The path to verify.</param>
    /// <returns>True if this is a child path of the parent path; else false.</returns>
    public bool InFolder(RelativePath other)
    {
        // Note: We assume equality to be separator and case insensitive
        //       therefore this property should transfer over to contains checks.
        var thisCopy = Path.Length <= 512 ? stackalloc char[Path.Length] : GC.AllocateUninitializedArray<char>(Path.Length);
        Path.CopyTo(thisCopy);
        thisCopy.NormalizeStringCaseAndPathSeparator();

        var otherCopy = other.Path.Length <= 512 ? stackalloc char[other.Path.Length] : GC.AllocateUninitializedArray<char>(other.Path.Length);
        other.Path.CopyTo(otherCopy);
        otherCopy.NormalizeStringCaseAndPathSeparator();

        if (!thisCopy.StartsWith(otherCopy)) return false;

        // If thisCopy is bigger make sure the next character is a path separator
        // So we don't assume that /foo/barbaz is in /foo/bar
        if (thisCopy.Length > otherCopy.Length)
        {
            var next = thisCopy[otherCopy.Length];
            return next is '\\' or '/';
        }
        return true;
    }

    /// <summary>
    /// Drops first X directories of a path.
    /// </summary>
    /// <param name="numDirectories">Number of directories to drop.</param>
    public RelativePath DropFirst(int numDirectories = 1)
    {
        // Normalize first
        var thisCopy = Path.Length <= 512 ? stackalloc char[Path.Length] : GC.AllocateUninitializedArray<char>(Path.Length);
        Path.CopyTo(thisCopy);

        // Now count in loop.
        var separatorChar = DetermineDirectorySeparatorChar();
        var currentIndex = 0;
        for (var x = 0; x < numDirectories; x++)
        {
            var foundIndex = thisCopy.SliceFast(currentIndex).IndexOf(separatorChar);
            currentIndex += foundIndex;

            if (foundIndex == -1)
            {
                ThrowHelpers.PathException($"Cannot drop first {numDirectories} directories in {Path}.");
                return default;
            }

            if (x == numDirectories - 1)
                break;

            currentIndex += 1;
        }

        return new RelativePath(Path[(currentIndex + 1)..]);
    }

    /// <summary>
    /// Determines the directory separator character used in this relative path between '\' and '/'.
    /// </summary>
    /// <returns>
    ///    Returns forward slash if found before a backslash, else backslash.
    /// </returns>
    public string DetermineDirectorySeparatorString() => IsDirectorySeparatorFrontSlash() ? "/" : "\\";

    /// <summary>
    /// Determines the directory separator character used in this relative path between '\' and '/'.
    /// </summary>
    /// <returns>
    ///    Returns forward slash if found before a backslash, else backslash.
    /// </returns>
    public char DetermineDirectorySeparatorChar() => IsDirectorySeparatorFrontSlash() ? '/' : '\\';

    /// <summary>
    /// Determines the directory separator character used in this relative path between '\' and '/'.
    /// </summary>
    /// <returns>
    ///    Returns true if separator is forward slash, else backslash.
    /// </returns>
    public bool IsDirectorySeparatorFrontSlash()
    {
        GetFirstDirectorySeparatorIndex(out var result);
        return result;
    }

    /// <summary>
    /// Determines the directory separator character used in this relative path between '\' and '/'.
    /// </summary>
    /// <param name="isFrontSlash">True if front slash is the separator, else back slash.</param>
    /// <returns>
    ///    Returns true if separator is forward slash, else backslash.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetFirstDirectorySeparatorIndex(out bool isFrontSlash) => PathHelpers.GetFirstDirectorySeparatorIndex(Path, out isFrontSlash);

    /// <summary>
    /// Returns a path relative to the sub-path specified.
    /// </summary>
    /// <param name="basePath">The sub-path specified.</param>
    public RelativePath RelativeTo(RelativePath basePath)
    {
        var other = basePath.Path;
        if (other.Length == 0)
            return this;

        // Note: We assume equality to be separator and case insensitive
        //       therefore this property should transfer over to contains checks.
        var thisCopy = Path.Length <= 512 ? stackalloc char[Path.Length] : GC.AllocateUninitializedArray<char>(Path.Length);
        Path.CopyTo(thisCopy);
        thisCopy.NormalizeStringCaseAndPathSeparator();

        var otherCopy = other.Length <= 512 ? stackalloc char[other.Length] : GC.AllocateUninitializedArray<char>(other.Length);
        other.CopyTo(otherCopy);
        otherCopy.NormalizeStringCaseAndPathSeparator();

        if (!thisCopy.StartsWith(otherCopy))
        {
            ThrowHelpers.PathException("Can't create path relative to paths that aren't in the same folder");
            return default;
        }

        return new RelativePath(Path[(otherCopy.Length + 1)..]);
    }

    /// <inheritdoc />
    public override string ToString() => Path;

    #region Equals & GetHashCode
    /// <inheritdoc />
    public bool Equals(RelativePath other)
    {
        return StringExtensions.CompareStringsCaseAndSeparatorInsensitive(Path, other.Path);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RelativePath other && Equals(other);
    }

    /// <inheritdoc />
    [SkipLocalsInit]
    public override int GetHashCode()
    {
        var thisCopy = Path.Length <= 512 ? stackalloc char[Path.Length] : GC.AllocateUninitializedArray<char>(Path.Length);
        Path.CopyTo(thisCopy);
        thisCopy.NormalizeStringCaseAndPathSeparator();
        return ((ReadOnlySpan<char>)thisCopy).GetNonRandomizedHashCode32();
    }
    #endregion

    /// <summary/>
    public static implicit operator string(RelativePath d) => d.Path;

    /// <summary/>
    public static implicit operator ReadOnlySpan<char>(RelativePath d) => d.Path;

    /// <summary/>
    public static implicit operator RelativePath(string b) => new(b);

    /// <summary/>
    public static bool operator ==(RelativePath lhs, RelativePath rhs) => lhs.Equals(rhs);

    /// <summary/>
    public static bool operator !=(RelativePath lhs, RelativePath rhs) => !(lhs == rhs);

    /// <inheritdoc />
    public int CompareTo(RelativePath other)
    {
        var aCopy = Path.Length <= 512 ? stackalloc char[Path.Length] : GC.AllocateUninitializedArray<char>(Path.Length);
        Path.CopyTo(aCopy);
        aCopy.NormalizeStringCaseAndPathSeparator();

        var bCopy = other.Path.Length <= 512 ? stackalloc char[other.Path.Length] : GC.AllocateUninitializedArray<char>(other.Path.Length);
        other.Path.CopyTo(bCopy);
        bCopy.NormalizeStringCaseAndPathSeparator();

        return MemoryExtensions.CompareTo(aCopy, bCopy, StringComparison.Ordinal);
    }
}

/// <summary>
/// Compares two relative paths for sorting purposes.
/// </summary>
public struct RelativePathComparer : IComparer<RelativePath>
{
    /// <inheritdoc />
    public int Compare(RelativePath x, RelativePath y) => x.CompareTo(y);
}
