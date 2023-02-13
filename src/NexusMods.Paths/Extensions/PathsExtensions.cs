using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Extensions;

/// <summary>
/// This class contains extension methods/common code for various classes implementing support for paths.
/// </summary>
public static class PathsExtensions
{
    /// <summary>
    /// Given a string that represents individual parts (such as those sourced from <see cref="RelativePath"/>
    /// and/or <see cref="AbsolutePath"/>); returns the parent directory.
    /// </summary>
    /// <param name="parts">The individual parts that represent the string</param>
    internal static string[] GetPathParent(this string[] parts)
    {
        if (parts.Length <= 1)
            ThrowHelpers.PathException($"Path does not have a parent folder");

        var newParts = GC.AllocateUninitializedArray<string>(parts.Length - 1);
        Array.Copy(parts, newParts, newParts.Length);
        return newParts;
    }
    
    /// <summary>
    /// Replaces the extension for a path that consists of a given set of parts.
    /// </summary>
    /// <param name="parts">The parts that constitute the path.</param>
    /// <param name="newExtension">The extension to add/replace in the parts.</param>
    internal static string[] ReplaceExtension(this string[] parts, Extension newExtension)
    {
        var paths = new string[parts.Length];
        Array.Copy(parts, paths, paths.Length);
        var oldName = paths[^1];
        var newName = ReplaceExtension(oldName, newExtension);
        paths[^1] = newName;
        return paths;
    }

    private static string ReplaceExtension(string oldName, Extension newExtension)
    {
        var nameLength = oldName.LastIndexOf(".", StringComparison.Ordinal);
        if (nameLength < 0)
        {
            // no file extension
            nameLength = oldName.Length;
        }

        var newName = oldName[..nameLength] + newExtension;
        return newName;
    }

    /// <summary>
    /// Creates a new path from the current one, appending an extension.
    /// </summary>
    /// <param name="parts">The parts to which the extension is to be appended to.</param>
    /// <param name="ext">The extension to append to the absolute path.</param>
    /// <returns></returns>
    internal static string[] WithExtension(this string[] parts, Extension ext)
    {
        var newParts = GC.AllocateUninitializedArray<string>(parts.Length);
        Array.Copy(parts, newParts, parts.Length);
        newParts[^1] += ext;
        return newParts;
    }
    
    /// <summary>
    /// Joins a number of relative paths into a combined path.
    /// </summary>
    /// <param name="paths">Paths; can be either of type <see cref="string"/> or <see cref="RelativePath"/>.</param>
    /// <returns></returns>
    /// <exception cref="PathException"></exception>
    internal static RelativePath[] JoinRelativePathsWithUnknownTypes(this object[] paths)
    {
        var converted = paths.Select(p =>
        {
            switch (p)
            {
                case string s:
                    return (RelativePath)s;
                    
                case RelativePath path:
                    return path;
                
                default:
                    ThrowHelpers.PathException($"Cannot cast {p} of type {p.GetType()} to Path");
                    return default;
            }
        }).ToArray();
        
        return converted;
    }
    
    /// <summary>
    /// Joins an existing set of parts with a set of supplied relative paths.
    /// </summary>
    internal static string[] AppendRelativePaths(this RelativePath[] paths, string[] parts)
    {
        var newLen = parts.Length + paths.Sum(p => p.Parts.Length);
        var newParts = GC.AllocateUninitializedArray<string>(newLen);
        Array.Copy(parts, newParts, parts.Length);

        var toIdx = parts.Length;
        foreach (var p in paths)
        {
            Array.Copy(p.Parts, 0, newParts, toIdx, p.Parts.Length);
            toIdx += p.Parts.Length;
        }

        return newParts;
    }
}