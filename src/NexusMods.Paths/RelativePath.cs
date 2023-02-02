namespace NexusMods.Paths;

public struct RelativePath : IPath, IEquatable<RelativePath>, IComparable<RelativePath>
{
    public readonly string[] Parts = Array.Empty<string>();

    private int _hashCode = 0;

    internal RelativePath(string[] parts)
    {
        Parts = parts;
    }

    public static RelativePath FromParts(string[] parts)
    {
        return new RelativePath(parts);
    }

    public static explicit operator RelativePath(string i)
    {
        var splits = i.Split(AbsolutePath.StringSplits, StringSplitOptions.RemoveEmptyEntries);
        if (splits.Length >= 1 && splits[0].Contains(':'))
            throw new PathException($"Tried to parse `{i} but `:` not valid in a path name");
        return new RelativePath(splits);
    }

    public static explicit operator string(RelativePath i)
    {
        return i.ToString();
    }

    public Extension Extension => Extension.FromPath(Parts[^1]);
    public RelativePath FileName => Parts.Length == 1 ? this : new RelativePath(new[] { Parts[^1] });

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
        var nameLength = oldName.LastIndexOf(".", StringComparison.CurrentCultureIgnoreCase);
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

    public override string ToString()
    {
        if (Parts == null || Parts.Length == 0) return "";
        return string.Join('\\', Parts);
    }

    public override int GetHashCode()
    {
        if (_hashCode != 0) return _hashCode;
        if (Parts == null || Parts.Length == 0) return -1;

        _hashCode = Parts.Aggregate(0,
            (current, part) => current ^ part.GetHashCode(StringComparison.CurrentCultureIgnoreCase));
        return _hashCode;
    }

    public bool Equals(RelativePath other)
    {
        if (other.Parts?.Length != Parts?.Length) return false;
        for (var idx = 0; idx < Parts?.Length; idx++)
            if (!Parts[idx].Equals(other.Parts?[idx], StringComparison.InvariantCultureIgnoreCase))
                return false;
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is RelativePath other && Equals(other);
    }

    public static bool operator ==(RelativePath a, RelativePath b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(RelativePath a, RelativePath b)
    {
        return !a.Equals(b);
    }

    public int CompareTo(RelativePath other)
    {
        return ArrayExtensions.CompareString(Parts, other.Parts);
    }

    public int Depth => Parts.Length;

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

    public RelativePath TopParent => new(Parts[..1]);
    public RelativePath FileNameWithoutExtension => Parts[^1][..^Extension.Length].ToRelativePath();
    public static RelativePath Empty => new(Array.Empty<string>());
    public static IComparer<RelativePath> Comparer => new RelativePathComparer();

    private class RelativePathComparer : IComparer<RelativePath>
    { 
        public int Compare(RelativePath x, RelativePath y)
        {
            return x.CompareTo(y);
        }
    }

    public readonly bool FileNameEndsWith(string postfix)
    {
        return Parts[^1].EndsWith(postfix, StringComparison.CurrentCultureIgnoreCase);
    }

    public bool FileNameStartsWith(string prefix)
    {
        return Parts[^1].StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase);
    }

    public bool StartsWith(string s)
    {
        return ToString().StartsWith(s, StringComparison.InvariantCultureIgnoreCase);
    }
}