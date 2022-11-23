using NexusMods.Paths;

namespace NexusMods.Hashing.xxHash64;

public readonly struct HashRelativePath : IPath, IEquatable<HashRelativePath>, IComparable<HashRelativePath>
{
    public readonly Hash Hash;
    public readonly RelativePath[] Parts;


    public Extension Extension => Parts.Length > 0
        ? Parts[^1].Extension
        : throw new InvalidOperationException("No path in HashRelativePath");

    public RelativePath FileName => Parts.Length > 0
        ? Parts[^1].FileName
        : throw new InvalidOperationException("No path in HashRelativePath");

    public HashRelativePath(Hash basePath, params RelativePath[] parts)
    {
        Hash = basePath;
        Parts = parts;
    }

    public override string ToString()
    {
        return Hash + "|" + string.Join("|", Parts);
    }

    public override bool Equals(object? obj)
    {
        return obj is HashRelativePath path && Equals(path);
    }

    public override int GetHashCode()
    {
        return Parts.Aggregate(Hash.GetHashCode(), (i, path) => i ^ path.GetHashCode());
    }

    public bool Equals(HashRelativePath other)
    {
        if (other.Parts.Length != Parts.Length) return false;
        if (other.Hash != Hash) return false;
        return ArrayExtensions.AreEqual(Parts, 0, other.Parts, 0, Parts.Length);
    }

    public int CompareTo(HashRelativePath other)
    {
        var init = Hash.CompareTo(other.Hash);
        if (init != 0) return init;
        return ArrayExtensions.Compare(Parts, other.Parts);
    }

    public static bool operator ==(HashRelativePath a, HashRelativePath b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(HashRelativePath a, HashRelativePath b)
    {
        return !a.Equals(b);
    }
}