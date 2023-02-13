namespace NexusMods.Paths;

/// <summary>
/// Abstracts an individual path.
/// </summary>
public interface IPath
{
    /// <summary>
    /// Gets the extension of this path.
    /// </summary>
    Extension Extension { get; }
    
    /// <summary>
    /// Gets the file name of this path.
    /// </summary>
    RelativePath FileName { get; }
}