using System.Runtime.CompilerServices;
using System.Text;
using NexusMods.Paths.Extensions;

[assembly: InternalsVisibleTo("NexusMods.Paths.Tests")]
namespace NexusMods.Paths.New;

/// <summary>
/// A path that represents a full path to a file or directory.
/// </summary>
public struct AbsolutePath2 : IEquatable<AbsolutePath2>
{
    private const char PathSeparatorForInternalOperations = '/';
    private static readonly string DirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();

    /// <summary>
    /// Contains the path to the directory inside which this absolute path is contained in.  
    /// </summary>
    /// <remarks>
    ///    Shouldn't end with a backslash.
    /// </remarks>
    public string? Directory { get; private set; }

    /// <summary>
    /// Contains the name of the file.  
    /// </summary>
    /// <remarks>
    ///    If <see cref="Directory"/> is null; this structure contains the full file path.  
    /// </remarks>
    public string FileName { get; private set; }

    /// <summary/>
    /// <param name="directory">
    ///     Name of the directory in question.
    ///     If possible, make sure doesn't end with backslash for optimal performance.
    /// </param>
    /// <param name="fileName">Name of the file. Full path if directory is null.</param>
    internal AbsolutePath2(string? directory, string fileName)
    {
        if (!string.IsNullOrEmpty(directory))
        {
            Directory = directory.EndsWith(Path.DirectorySeparatorChar) 
                ? directory[..^1] 
                : directory;
        }
        FileName = fileName;
    }

    /// <summary>
    /// Converts an existing full path into an absolute path.
    /// </summary>
    /// <param name="fullPath">The full path to use.</param>
    /// <returns>The converted absolute path.</returns>
    /// <remarks>
    ///    Do not use this API when searching/enumerating files; instead use <see cref="FromDirectoryAndFileName"/>.
    /// </remarks>
    public static AbsolutePath2 FromFullPath(string fullPath) => new(null, fullPath);

    /// <summary>
    /// Converts an existing full path into an absolute path.
    /// </summary>
    /// <param name="directoryPath">The path to the directory used.</param>
    /// <param name="fullPath">The full path to use.</param>
    /// <returns>The converted absolute path.</returns>
    public static AbsolutePath2 FromDirectoryAndFileName(string directoryPath, string fullPath) => new(directoryPath, fullPath);

    /// <summary>
    /// Returns the full path of the combined string.
    /// </summary>
    /// <returns>The full combined path.</returns>
    public string GetFullPath()
    {
        if (string.IsNullOrEmpty(Directory))
            return FileName;

        if (FileName.Length == 0)
            return Directory;
        
        return string.Concat(Directory, DirectorySeparatorChar, FileName);
    }

    /// <summary>
    /// Returns the full path of the combined string.
    /// </summary>
    /// <param name="buffer">
    ///    The buffer which the resulting string will be stored inside.  
    ///    Should at least be <see cref="GetFullPathLength"/> long.  
    /// </param>
    /// <returns>
    ///     The full combined path.
    /// If the buffer is not long enough; an empty path.
    /// </returns>
    /// <remarks>
    ///    If <see cref="Directory"/> is null; might return different buffer than passed in via parameter.
    /// </remarks>
    public ReadOnlySpan<char> GetFullPath(Span<char> buffer)
    {
        if (string.IsNullOrEmpty(Directory))
            return FileName;

        if (FileName.Length == 0)
            return Directory;
        
        var requiredLength = Directory.Length + FileName.Length + 1;
        if (buffer.Length < requiredLength)
            return default;

        Directory.CopyTo(buffer);
        buffer[Directory.Length] = Path.DirectorySeparatorChar;
        FileName.CopyTo(buffer.SliceFast(Directory.Length + 1));
        return buffer.SliceFast(0, requiredLength);
    }

    /// <summary>
    /// Returns the expected length of the full path.
    /// </summary>
    /// <returns></returns>
    public int GetFullPathLength()
    {
        if (Directory == null)
            return FileName.Length;

        return Directory.Length + FileName.Length + 1;
    }

    /// <summary>
    /// Joins the current absolute path with a relative path.
    /// </summary>
    /// <param name="path">
    ///    The relative path; stored case insensitive.
    /// </param>
    public AbsolutePath2 CombineChecked(RelativePath2 path)
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
        var relativeSpan = relativeOrig.Length <= 512 ? stackalloc char[relativeOrig.Length] 
                                                      : GC.AllocateUninitializedArray<char>(relativeOrig.Length);
        
        relativeOrig.CopyTo(relativeSpan);
        relativeSpan.Replace('\\', PathSeparatorForInternalOperations, relativeSpan);
        if (relativeSpan[0] == PathSeparatorForInternalOperations)
            relativeSpan = relativeSpan.SliceFast(1);
        
        // Now walk the directories.
        return FromFullPath(AppendChecked(Directory, relativeSpan));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AppendChecked(string path, Span<char> relativeSpan)
    {
        ReadOnlySpan<char> remainingPath = relativeSpan;
        ReadOnlySpan<char> splitSpan = default;
        while ((splitSpan = SplitDir(remainingPath)) != remainingPath)
        {
            path += $"{Path.DirectorySeparatorChar}{FindFileOrDirectoryCasing(path, splitSpan)}";
            remainingPath = remainingPath[(splitSpan.Length + 1)..];
        }

        if (remainingPath.Length > 0)
            path += $"{Path.DirectorySeparatorChar}{FindFileOrDirectoryCasing(path, remainingPath)}";
        
        return path;
    }
    
    /// <inheritdoc />
    public override string ToString() => GetFullPath();
    
    #region Equals & GetHashCode
    // Implementation Note:
    //    Directory is already normalised in this struct because.
    //    - Paths are sourced from the OS.
    //    - Paths where RelativePath was joined to AbsolutePath are normalised as part of the join (CombineChecked) process.
    //    Therefore we can compare ordinal.

    /// <inheritdoc />
    public bool Equals(AbsolutePath2 other)
    {
        // Ordinal.
        return Directory == other.Directory && 
               FileName == other.FileName;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is AbsolutePath2 other && 
               Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Ordinal.
        return HashCode.Combine(Directory, FileName);
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
        foreach (var entry in System.IO.Directory.EnumerateFileSystemEntries(searchDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            var entryFileName = Path.GetFileName(entry.AsSpan());
            if (fileName.Equals(entryFileName, StringComparison.OrdinalIgnoreCase))
                return entryFileName;
        }

        return fileName;
    }
}