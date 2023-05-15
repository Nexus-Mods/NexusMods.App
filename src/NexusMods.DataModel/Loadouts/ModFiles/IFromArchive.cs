using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// Used to indicate that a mod file will be installed from an archive.
/// </summary>
public interface IFromArchive
{
    /// <summary>
    /// The size of the file.
    /// </summary>
    public Size Size { get; }
    
    /// <summary>
    /// The hash of the file.
    /// </summary>
    public Hash Hash { get; }
    
    /// <summary>
    /// The relative path of the file inside the archive.
    /// </summary>
    public HashRelativePath From { get; }
}
