using System.Diagnostics;
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

        ReadOnlySpan<char> result = GetFileName(path.AsSpan());
        if (path.Length == result.Length)
            return path;

        return result.ToString();
    }
    
    /// <summary>
    /// The returned ReadOnlySpan contains the characters of the path that follows the last separator in path.
    /// </summary>
    public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
    {
        int root = GetPathRoot(path).Length;

        // We don't want to cut off "C:\file.txt:stream" (i.e. should be "file.txt:stream")
        // but we *do* want "C:Foo" => "Foo". This necessitates checking for the root.

        for (int i = path.Length; --i >= 0;)
        {
            if (i < root || IsDirectorySeparator(path[i]))
                return path.Slice(i + 1);
        }

        return path;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
    {
        return IsPathRooted(path) ? DirectorySeparatorCharAsString.AsSpan() : ReadOnlySpan<char>.Empty;
    }
    
    public static bool IsPathRooted([NotNullWhen(true)] string? path)
    {
        return path != null && IsPathRooted(path.AsSpan());
    }

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

        int end = GetDirectoryNameOffset(path.AsSpan());
        return end >= 0 ? path.Substring(0, end) : null;
    }
    
    internal static int GetRootLength(ReadOnlySpan<char> path)
    {
        return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
    }
    
    internal static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
    {
        int rootLength = GetRootLength(path);
        int end = path.Length;
        if (end <= rootLength)
            return -1;

        while (end > rootLength && !IsDirectorySeparator(path[--end])) ;

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
    internal static bool IsEffectivelyEmpty(string? path)
    {
        return string.IsNullOrEmpty(path);
    }

    internal static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
    {
        return path.IsEmpty;
    }
}