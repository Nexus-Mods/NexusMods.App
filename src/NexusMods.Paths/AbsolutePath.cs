using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// A path that represents a full path to a file or directory.
/// </summary>
public partial struct AbsolutePath : IPath, IComparable<AbsolutePath>, IEquatable<AbsolutePath>
{
    // TODO: Optimise the APIs AbsolutePath & RelativePath more.
    //       For now; this is postponed as the future of these APIs is uncertain;
    //       Tim and I are considering potentially obsoleting or reworking these APIs entirely.
    
    /// <summary>
    /// Represents an empty path.
    /// </summary>
    public static readonly AbsolutePath Empty = "".ToAbsolutePath();
    
    internal static readonly char[] StringSplits = { '/', '\\' };
    
    /// <summary>
    /// The format used for storage of this path, either Unix style or Windows style.
    /// </summary>
    public PathFormat PathFormat { get; }

    /// <summary>
    /// Individual components that make up the final assembled path.
    /// </summary>
    public readonly string[] Parts = Array.Empty<string>();

    /// <summary>
    /// Returns the extension of this path [by extracting it from the filename].  
    /// </summary>
    public Extension Extension => Extension.FromPath(Parts[^1]);
    
    /// <summary>
    /// Extracts the file name from this path.
    /// </summary>
    public RelativePath FileName => new(Parts[^1..]);
    
    /// <summary>
    /// Returns the parent path to the current path.
    /// i.e. The path '1 directory up'. (This is equivalent to <see cref="Path.GetDirectoryName(System.ReadOnlySpan{char})"/>)
    /// </summary>
    public AbsolutePath Parent => new(Parts.GetPathParent(), PathFormat);

    /// <summary>
    /// Returns the total amount of parts that constitute this path.
    /// </summary>
    public int Depth => Parts?.Length ?? 0;
    
    private int _hashCode = 0;

    internal AbsolutePath(string[] parts, PathFormat format)
    {
        Parts = parts;
        PathFormat = format;
    }

    /// <summary>
    /// Returns a <see cref="IEnumerable{T}"/> of this path and all of it's parents.
    /// </summary>
    public IEnumerable<AbsolutePath> ThisAndAllParents()
    {
        var p = this;
        while (true)
        {
            yield return p;
            if (p.Depth == 1)
                yield break;
            
            p = p.Parent;
        }
    }

    /// <summary>
    /// Returns a new path that is this path with the extension changed.
    /// </summary>
    /// <param name="newExtension">The extension to replace the old extension.</param>
    public readonly AbsolutePath ReplaceExtension(Extension newExtension) => new(Parts.ReplaceExtension(newExtension), PathFormat);

    /// <inheritdoc />
    public int CompareTo(AbsolutePath other)
    {
        return ArrayExtensions.CompareString(Parts, other.Parts);
    }
    
    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is AbsolutePath path && Equals(path);
    }

    /// <inheritdoc />
    public bool Equals(AbsolutePath other)
    {
        // We suppress nullable warning here because this is a struct.
        // If initialised to default under some circumstances; e.g. method parameters,
        // this may initialise null.
        var partsNull = Parts == null;
        var otherPartsNull = other.Parts == null!;
        
        if (partsNull && otherPartsNull) 
            return true;
        
        if (partsNull != otherPartsNull) 
            return false;

        if (other.Depth != Depth) 
            return false;
        
        for (var idx = 0; idx < Parts!.Length; idx++)
            if (!Parts[idx].Equals(other.Parts![idx], StringComparison.InvariantCultureIgnoreCase))
                return false;
        
        return true;
    }

    /// <summary>
    /// Extracts a relative path, given the base path supplied in the parameter.
    /// </summary>
    /// <param name="basePath">The path to extract.</param>
    /// <returns>The extracted path.</returns>
    public RelativePath RelativeTo(AbsolutePath basePath)
    {
        if (!ArrayExtensions.AreEqualIgnoreCase(basePath.Parts, 0, Parts, 0, basePath.Parts.Length))
            ThrowHelpers.PathException($"{basePath} is not a base path of {this}");

        var newParts = GC.AllocateUninitializedArray<string>(Parts.Length - basePath.Parts.Length);
        Array.Copy(Parts, basePath.Parts.Length, newParts, 0, newParts.Length);
        return new RelativePath(newParts);
    }

    /// <summary>
    /// Returns true if this path is a child of the given path.
    /// </summary>
    /// <param name="parent">The path to verify.</param>
    /// <returns>True if this is a child path of the parent path; else false.</returns>
    public bool InFolder(AbsolutePath parent)
    {
        return ArrayExtensions.AreEqualIgnoreCase(parent.Parts, 0, Parts, 0, parent.Parts.Length);
    }

    /// <summary>
    /// Combines this path with the given relative path(s).
    /// </summary>
    /// <param name="paths">Paths; can be either of type <see cref="string"/> or <see cref="RelativePath"/>.</param>
    public readonly AbsolutePath Join(params object[] paths) => Join(paths.JoinRelativePathsWithUnknownTypes());

    /// <summary>
    /// Combines this absolute path with a series of relative paths.
    /// </summary>
    /// <param name="paths">The array of relative paths to combine with the current path.</param>
    public readonly AbsolutePath Join(params RelativePath[] paths) => new(paths.AppendRelativePaths(Parts), PathFormat);
    
    /// <summary>
    /// Creates a new absolute path from the current one, appending an extension.
    /// </summary>
    /// <param name="ext">The extension to append to the absolute path.</param>
    /// <returns></returns>
    public AbsolutePath WithExtension(Extension ext) => new(Parts.WithExtension(ext), PathFormat);

    /// <summary>
    /// Appends a given string to the file name of this item [not extension].
    /// </summary>
    /// <param name="append">The string to append.</param>
    /// <returns></returns>
    public AbsolutePath AppendToName(string append)
    {
        return Parent.Join((FileName.FileNameWithoutExtension + append)
            .ToRelativePath()
            .WithExtension(Extension));
    }
    
    private static AbsolutePath Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) 
            return default;
        
        var parts = path.Split(StringSplits, StringSplitOptions.RemoveEmptyEntries);
        return new AbsolutePath(parts, DetectPathType(path));
    }

    private static bool IsDriveLetter(char c)
    {
        return c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }
    
    private static PathFormat DetectPathType(string path)
    {
        if (path.StartsWith("/"))
            return PathFormat.Unix;
        
        if (path.StartsWith(@"\\"))
            return PathFormat.Windows;

        if (path.Length >= 2 && IsDriveLetter(path[0]) && path[1] == ':')
            return PathFormat.Windows;

        throw new PathException($"Invalid Path format: {path}");
    }
    
    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_hashCode != 0) 
            return _hashCode;
        
        if (Parts == null || Parts.Length == 0) 
            return -1;

        var result = 0;
        foreach (var part in Parts)
            result ^= part.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
        
        _hashCode = result;
        return _hashCode;
    }
    
    /// <inheritdoc />
    public readonly override string ToString()
    {
        if (Parts == default) 
            return "";
        
        if (PathFormat != PathFormat.Windows) 
            return "/" + string.Join('/', Parts);
        
        return Parts.Length == 1 ? $"{Parts[0]}\\" : string.Join('\\', Parts);
    }
    
    /// <summary/>
    public static bool operator ==(AbsolutePath a, AbsolutePath b) => a.Equals(b);

    /// <summary/>
    public static bool operator !=(AbsolutePath a, AbsolutePath b) => !a.Equals(b);
    
    /// <summary>
    /// Converts a given string input into an absolute path
    /// </summary>
    public static explicit operator AbsolutePath(string input) => Parse(input);
}

/// <summary>
/// Flags if the path should use Unix or Windows path separators.
/// </summary>
public enum PathFormat : byte
{
    /// <summary>
    /// Uses back slashes for path separators.
    /// </summary>
    /// <remarks>
    ///    Modern iterations of Windows tend to support both backslash and forward slash.
    /// </remarks>
    Windows = 0,
    
    /// <summary>
    /// Uses forward slashes for path separators.
    /// </summary>
    Unix
}