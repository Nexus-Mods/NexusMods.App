using System.Runtime.CompilerServices;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

[assembly: InternalsVisibleTo("NexusMods.Paths.Tests")]
namespace NexusMods.Paths;

/// <summary>
/// A path that represents a full path to a file or directory.
/// </summary>
public partial struct AbsolutePath : IEquatable<AbsolutePath>, IPath
{
    private static readonly char PathSeparatorForInternalOperations = Path.DirectorySeparatorChar;
    private static readonly char SeparatorToReplace = PathSeparatorForInternalOperations == '/' ? '\\' : '/';

    private static readonly string DirectorySeparatorCharStr = Path.DirectorySeparatorChar.ToString();

    /// <summary>
    /// Contains the path to the directory inside which this absolute path is contained in.
    /// </summary>
    /// <remarks>
    ///    Shouldn't end with a backslash.
    /// </remarks>
    public string? Directory { get; private set; }

    /// <inheritdoc />
    public Extension Extension => Extension.FromPath(FileName);

    /// <inheritdoc />
    RelativePath IPath.FileName => Path.GetFileName(FileName);

    /// <summary>
    /// Contains the name of the file.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Gets the parent directory, i.e. navigates one folder up.
    /// </summary>
    public AbsolutePath Parent => FromFullPath(Path.GetDirectoryName(GetFullPath()) ?? "");

    /// <summary>
    /// Returns the file name without extensions
    /// </summary>
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FileName);

    /// <summary/>
    /// <param name="directory">
    ///     Name of the directory in question.
    ///     If possible, make sure doesn't end with backslash for optimal performance.
    /// </param>
    /// <param name="fileName">Name of the file. Full path if directory is null.</param>
    internal AbsolutePath(string? directory, string fileName)
    {
        // remove directory separator at the end of the directory
        // on Linux: don't do this if the directory is "/"
        if (!string.IsNullOrEmpty(directory))
        {
            Directory = directory.EndsWith(Path.DirectorySeparatorChar) && directory.Length != 1
                ? directory[..^1]
                : directory;
        }

        FileName = fileName;
    }

    /// <summary>
    /// Converts an existing full path into an absolute path.
    /// </summary>
    /// <param name="fullPath">The full path to use.</param>
    /// <returns>The converted absolute path.</returns>
    /// <remarks>
    ///    Do not use this API when searching/enumerating files; instead use <see cref="FromDirectoryAndFileName"/>.
    /// </remarks>
    public static AbsolutePath FromFullPath(string fullPath)
    {
        var index = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
        if (index < 0)
        {
            // No directory
            return new AbsolutePath(null, fullPath);
        }

        // Windows: "C:\foo", directory should be "C:"
        // Linux: "/foo", directory should be "/"
        var directory = index == 0 ? $"{fullPath[0]}": fullPath[..index];

        var fileName = fullPath[(index + 1)..];
        return new AbsolutePath(directory, fileName);
    }

    /// <summary>
    /// Converts an existing full path into an absolute path.
    /// </summary>
    /// <param name="directoryPath">The path to the directory used.</param>
    /// <param name="fullPath">The full path to use.</param>
    /// <returns>The converted absolute path.</returns>
    public static AbsolutePath FromDirectoryAndFileName(string? directoryPath, string fullPath) => new(directoryPath, fullPath);

    /// <summary>
    /// Returns the full path of the combined string.
    /// </summary>
    /// <returns>The full combined path.</returns>
    public readonly string GetFullPath()
    {
        if (string.IsNullOrEmpty(Directory))
            return FileName;

        if (FileName.Length == 0)
            return Directory;

        return string.Concat(Directory, DirectorySeparatorCharStr, FileName);
    }

    /// <summary>
    /// Returns the full path of the combined string.
    /// </summary>
    /// <param name="buffer">
    ///    The buffer which the resulting string will be stored inside.
    ///    Should at least be <see cref="GetFullPathLength"/> long.
    /// </param>
    /// <returns>
    ///     The full combined path.
    /// If the buffer is not long enough; an empty path.
    /// </returns>
    /// <remarks>
    ///    If <see cref="Directory"/> is null; might return different buffer than passed in via parameter.
    /// </remarks>
    public ReadOnlySpan<char> GetFullPath(Span<char> buffer)
    {
        if (string.IsNullOrEmpty(Directory))
            return FileName;

        if (FileName.Length == 0)
            return Directory;

        var requiredLength = Directory.Length + FileName.Length + 1;
        if (buffer.Length < requiredLength)
            return default;

        Directory.CopyTo(buffer);
        buffer[Directory.Length] = Path.DirectorySeparatorChar;
        FileName.CopyTo(buffer.SliceFast(Directory.Length + 1));
        return buffer.SliceFast(0, requiredLength);
    }

    /// <summary>
    /// Returns the expected length of the full path.
    /// </summary>
    /// <returns></returns>
    public int GetFullPathLength()
    {
        if (Directory == null)
            return FileName.Length;

        return Directory.Length + FileName.Length + 1;
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
    public readonly AbsolutePath CombineUnchecked(RelativePath path)
    {
        // Just in case.
        if (path.Path.Length <= 0)
            return this;

        // Since AbsolutePaths are created from the OS; we will assume the existing path is already correct, therefore
        // we only need to checked combine the relative path.
        var relativeOrig = path.Path;

        // Copy and normalise.
        var relativeSpan = relativeOrig.Length <= 512 ? stackalloc char[relativeOrig.Length]
            : GC.AllocateUninitializedArray<char>(relativeOrig.Length);

        relativeOrig.CopyTo(relativeSpan);
        relativeSpan.Replace(SeparatorToReplace, PathSeparatorForInternalOperations, relativeSpan);
        if (relativeSpan[0] == PathSeparatorForInternalOperations)
            relativeSpan = relativeSpan.SliceFast(1);

        // Now walk the directories.
        return FromFullPath($"{GetFullPath()}{DirectorySeparatorCharStr}{relativeSpan}");
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
        // Just in case.
        if (path.Path.Length <= 0)
            return this;

        // Note: Do not special case this for Windows.
        // We have a constraint where Directory and FileName are normalised
        // to follow OS casing. And Equals/GetHashCode relies on this.

        // Since AbsolutePaths are created from the OS; we will assume the existing path is already correct, therefore
        // we only need to checked combine the relative path.
        var relativeOrig = path.Path;

        // Copy and normalise.
        var relativeSpan = relativeOrig.Length <= 512 ? stackalloc char[relativeOrig.Length]
                                                      : GC.AllocateUninitializedArray<char>(relativeOrig.Length);

        relativeOrig.CopyTo(relativeSpan);
        relativeSpan.Replace(SeparatorToReplace, PathSeparatorForInternalOperations, relativeSpan);
        if (relativeSpan[0] == PathSeparatorForInternalOperations)
            relativeSpan = relativeSpan.SliceFast(1);

        // Now walk the directories.
        return FromFullPath(AppendChecked(GetFullPath(), relativeSpan));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AppendChecked(string path, Span<char> relativeSpan)
    {
        ReadOnlySpan<char> remainingPath = relativeSpan;
        ReadOnlySpan<char> splitSpan = default;
        while ((splitSpan = SplitDir(remainingPath)) != remainingPath)
        {
            path += $"{Path.DirectorySeparatorChar}{FindFileOrDirectoryCasing(path, splitSpan)}";
            remainingPath = remainingPath[(splitSpan.Length + 1)..];
        }

        if (remainingPath.Length > 0)
            path += $"{Path.DirectorySeparatorChar}{FindFileOrDirectoryCasing(path, remainingPath)}";

        return path;
    }

    /// <summary>
    /// Gets a path relative to another absolute path.
    /// </summary>
    /// <param name="other">The path from which the relative path should be made.</param>
    public RelativePath RelativeTo(AbsolutePath other)
    {
        var otherLength = other.GetFullPathLength();
        if (otherLength == 0)
            return new RelativePath(GetFullPath());

        // Note: We assume equality to be separator and case insensitive
        //       therefore this property should transfer over to contains checks.
        var thisPathLength = GetFullPathLength();
        var thisFullPath = thisPathLength <= 512 ? stackalloc char[thisPathLength] : GC.AllocateUninitializedArray<char>(thisPathLength);
        GetFullPath(thisFullPath);

        var otherPathLength = other.GetFullPathLength();
        var otherFullPath = otherPathLength <= 512 ? stackalloc char[otherPathLength] : GC.AllocateUninitializedArray<char>(otherPathLength);
        other.GetFullPath(otherFullPath);

        if (!thisFullPath.StartsWith(otherFullPath))
        {
            ThrowHelpers.PathException("Can't create path relative to paths that aren't in the same folder");
            return default;
        }

        return new RelativePath(thisFullPath.SliceFast(otherFullPath.Length + 1).ToString());
    }

    /// <summary>
    /// Creates a new absolute path from the current one, appending an extension.
    /// </summary>
    /// <param name="ext">The extension to append to the absolute path.</param>
    public AbsolutePath WithExtension(Extension ext) => FromDirectoryAndFileName(Directory, FileName + ext);

    /// <summary>
    /// Returns true if this path is a child of the specified path.
    /// </summary>
    /// <param name="parent">The path to verify.</param>
    /// <returns>True if this is a child path of the parent path; else false.</returns>
    [SkipLocalsInit]
    public bool InFolder(AbsolutePath parent)
    {
        // other
        var len = parent.GetFullPathLength();
        var parentStr = len <= 512 ? stackalloc char[len] : GC.AllocateUninitializedArray<char>(len);
        parent.GetFullPath(parentStr);

        // this
        len = GetFullPathLength();
        var thisStr = len <= 512 ? stackalloc char[len] : GC.AllocateUninitializedArray<char>(len);
        GetFullPath(thisStr);

        return thisStr.StartsWith(parentStr);
    }

    /// <summary>
    /// Replaces the extension used in this absolute path.
    /// </summary>
    /// <param name="ext">The extension to replace.</param>
    public AbsolutePath ReplaceExtension(Extension ext)
    {
        return FromDirectoryAndFileName(Directory!, FileName.ReplaceExtension(ext));
    }

    /// <summary/>
    public static bool operator ==(AbsolutePath lhs, AbsolutePath rhs) => lhs.Equals(rhs);

    /// <summary/>
    public static bool operator !=(AbsolutePath lhs, AbsolutePath rhs) => !(lhs == rhs);

    /// <inheritdoc />
    public override string ToString() => GetFullPath();

    #region Equals & GetHashCode
    // Implementation Note:
    //    Directory is already normalised in this struct because.
    //    - Paths are sourced from the OS.
    //    - Paths where RelativePath was joined to AbsolutePath are normalised as part of the join (CombineChecked) process.
    //    Therefore we can compare ordinal.

    /// <inheritdoc />
    public bool Equals(AbsolutePath other)
    {
        // Ordinal.
        return Directory == other.Directory &&
               FileName == other.FileName;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is AbsolutePath other &&
               Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Ordinal.
        var a = StringExtensions.GetNonRandomizedHashCode(Directory);
        var b = StringExtensions.GetNonRandomizedHashCode(FileName);
        return HashCode.Combine(a, b);
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
        var index = text.IndexOf(Path.DirectorySeparatorChar);
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

    /// <summary/>
    /// <returns>Full path with directory separator string attached at the end.</returns>
    private readonly string GetFullPathWithSeparator()
    {
        if (string.IsNullOrEmpty(Directory))
            return string.Concat(FileName, DirectorySeparatorCharStr);

        if (FileName.Length == 0)
            return string.Concat(Directory, DirectorySeparatorCharStr);

        return string.Concat(Directory, DirectorySeparatorCharStr, FileName, DirectorySeparatorCharStr);
    }
}
