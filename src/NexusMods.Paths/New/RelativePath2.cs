using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.New;

/// <summary>
/// A path that represents a partial path to a file or directory.  
/// </summary>
public struct RelativePath2 : IEquatable<RelativePath2>
{
    /// <summary>
    /// Contains the relative path stored in this instance.
    /// </summary>
    /// <remarks>
    public string Path { get; private set; }

    /// <summary>
    /// Creates a relative path given a string.
    /// </summary>
    /// <param name="path">The relative path to use.</param>
    public RelativePath2(string path)
    {
        Path = path;
    }
    
    #region Equals & GetHashCode
    
    /// <inheritdoc />
    public bool Equals(RelativePath2 other)
    {
        return StringExtensions.CompareStringsCaseAndSeparatorInsensitive(Path, other.Path);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RelativePath2 other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        Span<char> aCopy = Path.Length <= 512 ? stackalloc char[Path.Length] : GC.AllocateUninitializedArray<char>(Path.Length);
        aCopy.NormalizeStringCaseAndPathSeparator();
        return ((ReadOnlySpan<char>)aCopy).GetNonRandomizedHashCode32();
    }
    #endregion
}