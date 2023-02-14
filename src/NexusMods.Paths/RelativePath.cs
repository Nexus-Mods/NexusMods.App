using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// Represents a relative path.
/// </summary>
public struct RelativePath : IPath, IEquatable<RelativePath>, IComparable<RelativePath>
{
    /// <summary>
    /// Represents an empty path.
    /// </summary>
    public static RelativePath Empty => new(Array.Empty<string>());
    
    /// <summary>
    /// Used to compare relative paths for sorting purposes.
    /// </summary>
    public static IComparer<RelativePath> Comparer => new RelativePathComparer();
    
    /// <summary>
    /// Individual components of the path. 
    /// </summary>
    public readonly string[] Parts = Array.Empty<string>();
    
    /// <inheritdoc />
    public Extension Extension => Extension.FromPath(Parts[^1]);

    /// <inheritdoc />
    public RelativePath FileName => Parts.Length == 1 ? this : new RelativePath(new[] { Parts[^1] });
    
    /// <summary>
    /// Returns just the file name in this path, without extension.
    /// </summary>
    public RelativePath FileNameWithoutExtension => Parts[^1][..^Extension.Length].ToRelativePath();
    
    /// <summary>
    /// Returns the first part of the relative path, i.e. first folder/file in the path.
    /// </summary>
    public RelativePath TopParent => new(Parts[..1]);
    
    /// <summary>
    /// Amount of files + folders constituting the total path combined.
    /// </summary>
    public int Depth => Parts.Length;

    private int _hashCode = 0;

    internal RelativePath(string[] parts) => Parts = parts;

    /// <summary>
    /// Assembles a relative path from a specified number of individual parts.
    /// </summary>
    /// <param name="parts">
    ///     The parts that assemble.
    ///     Each part is a directory or file name, i.e. parts are delimited by <see cref="Path.DirectorySeparatorChar"/>.
    /// </param>
    public static RelativePath FromParts(string[] parts) => new(parts);

    /// <summary>
    /// Returns the parent path to the current path.
    /// i.e. The path '1 directory up'. (This is equivalent to <see cref="Path.GetDirectoryName(System.ReadOnlySpan{char})"/>)
    /// </summary>
    public RelativePath Parent => new(Parts.GetPathParent());

    /// <summary>
    /// Returns a new path that is this path with the extension changed.
    /// </summary>
    /// <param name="newExtension">The extension to replace the old extension.</param>
    public RelativePath ReplaceExtension(Extension newExtension) => new(Parts.ReplaceExtension(newExtension));

    /// <summary>
    /// Creates a new relative path from the current one, appending an extension.
    /// </summary>
    /// <param name="ext">The extension to append to the absolute path.</param>
    /// <returns></returns>
    public RelativePath WithExtension(Extension ext) => new(Parts.WithExtension(ext));

    /// <summary>
    /// Joins this relative path to an existing absolute path, forming a complete absolute path.
    /// </summary>
    /// <param name="basePath">The path to combine with.</param>
    /// <returns>The combined path.</returns>
    public AbsolutePath Combine(AbsolutePath basePath)
    {
        var newArray = GC.AllocateUninitializedArray<string>(basePath.Parts.Length + Parts.Length);
        Array.Copy(basePath.Parts, 0, newArray, 0, basePath.Parts.Length);
        Array.Copy(Parts, 0, newArray, basePath.Parts.Length, Parts.Length);
        return new AbsolutePath(newArray, basePath.PathFormat);
    }
    
    public RelativePath RelativeTo(RelativePath basePath)
    {
        if (!InFolder(basePath))
            throw new Exception("Can't create path relative to paths that aren't in the same folder");
        return new RelativePath(Parts[basePath.Parts.Length..]);
    }

    /// <summary>
    /// Returns true if this path is a child of the given path.
    /// </summary>
    /// <param name="parent">The path to verify.</param>
    /// <returns>True if this is a child path of the parent path; else false.</returns>
    public readonly bool InFolder(RelativePath parent)
    {
        return ArrayExtensions.AreEqualIgnoreCase(parent.Parts, 0, Parts, 0, parent.Parts.Length);
    }
    
    /// <summary>
    /// Combines this path with the given relative path(s).
    /// </summary>
    /// <param name="paths">Paths; can be either of type <see cref="string"/> or <see cref="RelativePath"/>.</param>
    public RelativePath Join(params object[] paths) => Join(paths.JoinRelativePathsWithUnknownTypes());

    /// <summary>
    /// Combines this absolute path with a series of relative paths.
    /// </summary>
    /// <param name="paths">The array of relative paths to combine with the current path.</param>
    public readonly RelativePath Join(params RelativePath[] paths) => new(paths.AppendRelativePaths(Parts));

    /// <summary>
    /// Determines whether the file name in this path ends with a specific string.
    /// </summary>
    /// <param name="suffix">The suffix to test.</param>
    public readonly bool FileNameEndsWith(ReadOnlySpan<char> suffix)
    {
        return Parts[^1].AsSpan().EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase);
    }
    
    /// <summary>
    /// Determines whether the file name in this string start string.
    /// </summary>
    /// <param name="prefix">The prefix to test.</param>
    public bool FileNameStartsWith(ReadOnlySpan<char> prefix)
    {
        return Parts[^1].AsSpan().StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Determines whether the beginning of this path matches the specified suffix.
    /// </summary>
    /// <param name="suffix">The suffix to test.</param>
    public bool StartsWith(ReadOnlySpan<char> suffix)
    {
        return ToString().AsSpan().StartsWith(suffix, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Parts == null || Parts.Length == 0)
            return "";
        
        return string.Join(Path.DirectorySeparatorChar, Parts);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_hashCode != 0) 
            return _hashCode;
        
        if (Parts == null || Parts.Length == 0) 
            return -1;

        _hashCode = Parts.Aggregate(0, (current, part) => current ^ part.GetHashCode(StringComparison.InvariantCultureIgnoreCase));
        return _hashCode;
    }

    /// <inheritdoc />
    public bool Equals(RelativePath other)
    {
        // See: AbsolutePath.Equals for the explanation as to why I'm ignoring this.
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (other.Parts?.Length != Parts?.Length)
            return false;
        
        for (var idx = 0; idx < Parts?.Length; idx++)
            if (!Parts[idx].Equals(other.Parts?[idx], StringComparison.InvariantCultureIgnoreCase))
                return false;
        
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RelativePath other && Equals(other);

    /// <inheritdoc />
    public int CompareTo(RelativePath other) => ArrayExtensions.CompareString(Parts, other.Parts);
    
    /// <summary/>
    public static bool operator ==(RelativePath a, RelativePath b) => a.Equals(b);

    /// <summary/>
    public static bool operator !=(RelativePath a, RelativePath b) => !a.Equals(b);

    /// <summary>
    /// Converts a string with an existing path into a relative path.
    /// </summary>
    public static explicit operator RelativePath(string i)
    {
        var splits = i.Split(AbsolutePath.StringSplits, StringSplitOptions.RemoveEmptyEntries);
        if (splits.Length >= 1 && splits[0].Contains(':'))
            throw new PathException($"Tried to parse `{i} but `:` not valid in a path name");
        
        return new RelativePath(splits);
    }

    /// <summary>
    /// Converts a relative path back to a string.
    /// </summary>
    public static explicit operator string(RelativePath i) => i.ToString();
    
    private class RelativePathComparer : IComparer<RelativePath>
    { 
        public int Compare(RelativePath x, RelativePath y) => x.CompareTo(y);
    }
}