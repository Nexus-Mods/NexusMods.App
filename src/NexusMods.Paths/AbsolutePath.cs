using System.Diagnostics;
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
[DebuggerDisplay("{DebugDisplay()}")]
public readonly partial struct AbsolutePath : IEquatable<AbsolutePath>, IPath
{
    private static readonly char PathSeparatorForInternalOperations = Path.DirectorySeparatorChar;
    private static readonly char SeparatorToReplace = PathSeparatorForInternalOperations == '/' ? '\\' : '/';

    private static readonly string DirectorySeparatorCharStr = Path.DirectorySeparatorChar.ToString();

    /// <summary>
    /// The directory component of the path.
    /// </summary>
    /// <remarks>
    /// This string is never empty and might end with a directory separator.
    /// This is only guaranteed for root directories, every other directory
    /// shall not have trailing directory separators.
    /// </remarks>
    public readonly string Directory;

    /// <summary>
    /// The characters after the last directory separator.
    /// </summary>
    /// <remarks>
    /// This string can be empty if the entire path is just a root directory.
    /// </remarks>
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

    internal AbsolutePath(string directory, string fileName, IFileSystem fileSystem)
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
        var rootLength = GetRootLength(span);
        if (rootLength == 0)
            throw new ArgumentException($"The provided path is not rooted: \"{fullPath}\"", nameof(fullPath));

        // path is only the root directory
        if (span.Length == rootLength)
            return new AbsolutePath(fullPath, "", fileSystem);

        var slice = span.SliceFast(rootLength);
        if (slice.DangerousGetReferenceAt(slice.Length - 1) == PathSeparatorForInternalOperations)
            slice = slice.SliceFast(0, slice.Length - 1);

        var separatorIndex = slice.LastIndexOf(PathSeparatorForInternalOperations);
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
        => FromDirectoryAndFileName(Directory, FileName.ReplaceExtension(ext), FileSystem);

    /// <summary>
    /// Returns true if the given directory is a root directory on the current
    /// platform.
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException">
    /// The current platform is not supported.
    /// </exception>
    internal static bool IsRootDirectory(ReadOnlySpan<char> directory)
    {
        var rootLength = GetRootLength(directory);
        return rootLength == directory.Length;
    }

    /// <summary>
    /// Gets the length of the root of the path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException">
    /// The current platform is not supported.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetRootLength(ReadOnlySpan<char> path)
    {
        if (OperatingSystem.IsLinux())
            return path.Length >= 1 && path[0] == PathSeparatorForInternalOperations ? 1 : 0;

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        // NOTE (erri120): UNC paths (\\?\C:\) and Device paths (\\?\.)
        // are not supported. Only classic drive specific paths: C:\

        // https://github.com/dotnet/runtime/blob/bb2b8605df0e916dcd6339f2056efb2bd4521ff5/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L223-L233
        if (path.Length < 3 || path[1] != ':' || !IsValidDriveChar(path[0])) return 0;
        return path[2] == PathSeparatorForInternalOperations ? 3 : 0;
    }

    /// <summary>
    /// Returns true if the given character is a valid drive letter.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidDriveChar(char value)
    {
        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.
        // https://github.com/dotnet/runtime/blob/main/LICENSE.TXT
        // source: https://github.com/dotnet/runtime/blob/d9f453924f7c3cca9f02d920a57e1477293f216e/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L69-L75
        return (uint)((value | 0x20) - 'a') <= 'z' - 'a';
    }

    /// <summary>
    /// Joins two path components together.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    internal static string JoinPathComponents(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length < 1) return string.Empty;
        if (left.DangerousGetReferenceAt(left.Length - 1) == PathSeparatorForInternalOperations)
            return string.Concat(left, right);

        ReadOnlySpan<char> separatorCharSpan = stackalloc char[1] { PathSeparatorForInternalOperations };
        return string.Concat(left, separatorCharSpan, right);
    }

    /// <summary>
    /// Returns the full path of the combined string.
    /// </summary>
    /// <returns>The full combined path.</returns>
    public string GetFullPath()
    {
        var requiredLength = GetFullPathLength();
        return string.Create(requiredLength, (Directory, FileName), (span, tuple) =>
        {
            var (directory, fileName) = tuple;
            GetFullPath(span, directory, fileName);
        });
    }

    /// <summary>
    /// Copies the full path into <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">The buffer that will store the full path. Has to be large enough
    /// to fit the full path. Use <see cref="GetFullPathLength"/> to get the required length.</param>
    /// <exception cref="ArgumentException">The buffer is too small.</exception>
    public void GetFullPath(Span<char> buffer)
    {
        var requiredLength = GetFullPathLength();
        if (buffer.Length < requiredLength)
            throw new ArgumentException($"Buffer is too small: {buffer.Length} < {requiredLength}");
        GetFullPath(buffer, Directory, FileName);
    }

    private static void GetFullPath(Span<char> span, ReadOnlySpan<char> directory, ReadOnlySpan<char> fileName)
    {
        directory.CopyTo(span);
        if (fileName.Length == 0) return;

        if (directory.DangerousGetReferenceAt(directory.Length - 1) == PathSeparatorForInternalOperations)
        {
            fileName.CopyTo(span.SliceFast(directory.Length));
        }
        else
        {
            span[directory.Length] = PathSeparatorForInternalOperations;
            fileName.CopyTo(span.SliceFast(directory.Length + 1));
        }
    }

    /// <summary>
    /// Returns the expected length of the full path.
    /// </summary>
    /// <returns></returns>
    public int GetFullPathLength()
    {
        if (FileName.Length == 0) return Directory.Length;

        var rootLength = GetRootLength(Directory);
        if (rootLength == Directory.Length)
        {
            return rootLength + FileName.Length;
        }

        return Directory.Length + DirectorySeparatorCharStr.Length + FileName.Length;
    }


    /// <summary>
    /// Obtains the name of the first folder stored in this path.
    /// </summary>
    public AbsolutePath GetRootDirectory()
    {
        var span = Directory.AsSpan();

        var rootLength = GetRootLength(span);
        if (rootLength == 0) return FromFullPath(Directory, FileSystem);

        var slice = span.SliceFast(0, rootLength);
        return FromFullPath(slice.ToString(), FileSystem);
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

        // remove leading separator character
        if (relativeSpan[0] == PathSeparatorForInternalOperations)
            relativeSpan = relativeSpan.SliceFast(1);

        var newPath = JoinPathComponents(GetFullPath(), relativeSpan);
        return FromFullPath(newPath, FileSystem);
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
        var relativeSpan = relativeOrig.Length <= 512
            ? stackalloc char[relativeOrig.Length]
            : GC.AllocateUninitializedArray<char>(relativeOrig.Length);

        relativeOrig.CopyTo(relativeSpan);
        relativeSpan.Replace(SeparatorToReplace, PathSeparatorForInternalOperations, relativeSpan);

        if (relativeSpan[0] == PathSeparatorForInternalOperations)
            relativeSpan = relativeSpan.SliceFast(1);

        // Now walk the directories.
        return FromFullPath(AppendChecked(GetFullPath(), relativeSpan), FileSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AppendChecked(string path, Span<char> relativeSpan)
    {
        ReadOnlySpan<char> remainingPath = relativeSpan;
        ReadOnlySpan<char> splitSpan;
        while ((splitSpan = SplitDir(remainingPath)) != remainingPath)
        {
            path = JoinPathComponents(path, FindFileOrDirectoryCasing(path, splitSpan));
            remainingPath = remainingPath[(splitSpan.Length + 1)..];
        }

        if (remainingPath.Length > 0)
            path = JoinPathComponents(path, FindFileOrDirectoryCasing(path, remainingPath));

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

        var rootLength = GetRootLength(otherFullPath);
        if (rootLength == otherPathLength)
            return new RelativePath(thisFullPath.SliceFast(rootLength).ToString());

        return new RelativePath(thisFullPath.SliceFast(otherFullPath.Length + 1).ToString());
    }

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


    /// <summary/>
    public static bool operator ==(AbsolutePath lhs, AbsolutePath rhs) => lhs.Equals(rhs);

    /// <summary/>
    public static bool operator !=(AbsolutePath lhs, AbsolutePath rhs) => !(lhs == rhs);

    /// <inheritdoc />
    public override string ToString()
    {
        if (this == default)
            return "<default>";
        return GetFullPath();
    }

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
        var index = text.IndexOf(PathSeparatorForInternalOperations);
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

#if DEBUG
    private string DebugDisplay()
    {
        return IsRootDirectory(Directory)
            ? $"{Directory}{FileName}"
            : $"{Directory}{DirectorySeparatorCharStr}{FileName}";
    }
#endif
}
