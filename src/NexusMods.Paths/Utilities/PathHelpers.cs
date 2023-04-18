using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NexusMods.Paths.Utilities;

/// <summary>
/// Several of these functions are ported and slightly modified from .NET's repo in order to support
/// `/` and `\` on Linux/OSX
/// </summary>
public static class PathHelpers
{
    internal const char DirectorySeparatorChar = '/';
    internal const char AltDirectorySeparatorChar = '\\';
    internal const char VolumeSeparatorChar = '/';
    internal const char PathSeparator = ':';
    internal const string DirectorySeparatorCharAsString = "/";

    /// <summary>
    /// Returns the name and extension parts of the given path. The resulting string contains
    /// the characters of path that follow the last separator in path. The resulting string is
    /// null if path is null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetFileName(string? path)
    {
        if (path == null)
            return null;

        var result = GetFileName(path.AsSpan());
        if (path.Length == result.Length)
            return path;

        return result.ToString();
    }

    /// <summary>
    /// The returned ReadOnlySpan contains the characters of the path that follows the last separator in path.
    /// </summary>
    public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
    {
        var root = GetPathRoot(path).Length;

        // We don't want to cut off "C:\file.txt:stream" (i.e. should be "file.txt:stream")
        // but we *do* want "C:Foo" => "Foo". This necessitates checking for the root.

        for (var i = path.Length; --i >= 0;)
        {
            if (i < root || IsDirectorySeparator(path[i]))
                return path.Slice(i + 1);
        }

        return path;
    }

    /// <summary>
    /// Returns the root portion of the given path. The resulting string
    /// consists of those rightmost characters of the path that constitute the
    /// root of the path. Possible patterns for the resulting string are: An
    /// empty string (a relative path on the current drive), "\" (an absolute
    /// path on the current drive), "X:" (a relative path on a given drive,
    /// where X is the drive letter), "X:\" (an absolute path on a given drive),
    /// and "\\server\share" (a UNC path for a given server and share name).
    /// The resulting string is null if path is null. If the path is empty or
    /// only contains whitespace characters an ArgumentException gets thrown.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
    {
        return IsPathRooted(path) ? DirectorySeparatorCharAsString.AsSpan() : ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Tests if the given path contains a root. A path is considered rooted
    /// if it starts with a backslash ("\") or a valid drive letter and a colon (":").
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPathRooted(ReadOnlySpan<char> path)
    {
        return path.Length > 0 && path[0] == DirectorySeparatorChar;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDirectorySeparator(char c)
    {
        return c is DirectorySeparatorChar or AltDirectorySeparatorChar;
    }

    /// <summary>
    /// Returns the directory portion of a file path. This method effectively
    /// removes the last segment of the given file path, i.e. it returns a
    /// string consisting of all characters up to but not including the last
    /// backslash ("\") in the file path. The returned value is null if the
    /// specified path is null, empty, or a root (such as "\", "C:", or
    /// "\\server\share").
    /// </summary>
    /// <remarks>
    /// Directory separators are normalized in the returned string.
    /// </remarks>
    public static string? GetDirectoryName(string? path)
    {
        if (path == null || IsEffectivelyEmpty(path.AsSpan()))
            return null;

        var end = GetDirectoryNameOffset(path.AsSpan());
        return end >= 0 ? path.Substring(0, end) : null;
    }

    /// <summary>
    /// Determines the directory separator character used in this relative path between '\' and '/'.
    /// </summary>
    /// <param name="path">The path to get separator index from.</param>
    /// <param name="isFrontSlash">True if front slash is the separator, else back slash.</param>
    /// <returns>
    ///    Returns true if separator is forward slash, else backslash.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFirstDirectorySeparatorIndex(ReadOnlySpan<char> path, out bool isFrontSlash)
    {
        var frontIdx = path.IndexOf('/');
        var backIdx = path.IndexOf('\\');

        if (frontIdx > backIdx && frontIdx >= 0)
        {
            isFrontSlash = true;
            return frontIdx;
        }

        isFrontSlash = false;
        return backIdx;
    }
    
    /// <summary>
    /// Returns true if a given path (using either '/' or '\' separator) has a subdirectory.
    /// </summary>
    /// <param name="path">The path to get subdirectory from.</param>
    /// <returns>
    ///    Returns true if subdirectory exists, else false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PathHasSubdirectory(ReadOnlySpan<char> path)
    {
        return GetFirstDirectorySeparatorIndex(path, out _) >= 0;
    }

    private static int GetRootLength(ReadOnlySpan<char> path)
    {
        return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
    }

    private static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
    {
        var rootLength = GetRootLength(path);
        var end = path.Length;
        if (end <= rootLength)
            return -1;

        while (end > rootLength && !IsDirectorySeparator(path[--end])) { }

        // Trim off any remaining separators (to deal with C:\foo\\bar)
        while (end > rootLength && IsDirectorySeparator(path[end - 1]))
            end--;

        return end;
    }

    /// <summary>
    /// Returns true if the path is effectively empty for the current OS.
    /// For unix, this is empty or null. For Windows, this is empty, null, or
    /// just spaces ((char)32).
    /// </summary>
    internal static bool IsEffectivelyEmpty(ReadOnlySpan<char> path) => path.IsEmpty;
}
