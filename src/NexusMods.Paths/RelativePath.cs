namespace NexusMods.Paths;

/// <summary>
/// Represents a relative path.
/// </summary>
public struct RelativePath : IPath, IEquatable<RelativePath>, IComparable<RelativePath>
{
    /// <summary>
    /// Individual components of the path. 
    /// </summary>
    public readonly string[] Parts = Array.Empty<string>();
    
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


    public Extension Extension => Extension.FromPath(Parts[^1]);
    
    public RelativePath FileName => Parts.Length == 1 ? this : new RelativePath(new[] { Parts[^1] });
    
    public RelativePath TopParent => new(Parts[..1]);
    
    public RelativePath FileNameWithoutExtension => Parts[^1][..^Extension.Length].ToRelativePath();
    
    public static RelativePath Empty => new(Array.Empty<string>());
    
    public static IComparer<RelativePath> Comparer => new RelativePathComparer();
    
    public RelativePath Parent
    {
        get
        {
            if (Parts.Length <= 1)
                throw new PathException("Can't get parent of a top level path");

            var newParts = new string[Parts.Length - 1];
            Array.Copy(Parts, newParts, Parts.Length - 1);
            return new RelativePath(newParts);
        }
    }


    public RelativePath ReplaceExtension(Extension newExtension)
    {
        var paths = new string[Parts.Length];
        Array.Copy(Parts, paths, paths.Length);
        var oldName = paths[^1];
        var newName = ReplaceExtension(oldName, newExtension);
        paths[^1] = newName;
        return new RelativePath(paths);
    }
    
    internal static string ReplaceExtension(string oldName, Extension newExtension)
    {
        var nameLength = oldName.LastIndexOf(".", StringComparison.Ordinal);
        if (nameLength < 0)
        {
            // no file extension
            nameLength = oldName.Length;
        }

        var newName = oldName.Substring(0, nameLength) + newExtension;
        return newName;
    }

    public RelativePath WithExtension(Extension ext)
    {
        var parts = new string[Parts.Length];
        Array.Copy(Parts, parts, Parts.Length);
        parts[^1] += ext;
        return new RelativePath(parts);
    }

    public AbsolutePath RelativeTo(AbsolutePath basePath)
    {
        var newArray = new string[basePath.Parts.Length + Parts.Length];
        Array.Copy(basePath.Parts, 0, newArray, 0, basePath.Parts.Length);
        Array.Copy(Parts, 0, newArray, basePath.Parts.Length, Parts.Length);
        return new AbsolutePath(newArray, basePath.PathFormat);
    }

    public readonly bool InFolder(RelativePath parent)
    {
        return ArrayExtensions.AreEqualIgnoreCase(parent.Parts, 0, Parts, 0, parent.Parts.Length);
    }
    
    public RelativePath Join(params object[] paths)
    {
        var converted = paths.Select(p =>
        {
            return p switch
            {
                string s => (RelativePath)s,
                RelativePath path => path,
                _ => throw new PathException($"Cannot cast {p} of type {p.GetType()} to Path")
            };
        }).ToArray();
        return Join(converted);
    }

    public readonly RelativePath Join(params RelativePath[] paths)
    {
        var newLen = Parts.Length + paths.Sum(p => p.Parts.Length);
        var newParts = new string[newLen];
        Array.Copy(Parts, newParts, Parts.Length);

        var toIdx = Parts.Length;
        foreach (var p in paths)
        {
            Array.Copy(p.Parts, 0, newParts, toIdx, p.Parts.Length);
            toIdx += p.Parts.Length;
        }

        return new RelativePath(newParts);
    }
    
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
    
    private class RelativePathComparer : IComparer<RelativePath>
    { 
        public int Compare(RelativePath x, RelativePath y)
        {
            return x.CompareTo(y);
        }
    }

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
}