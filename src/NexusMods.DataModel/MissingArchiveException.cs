using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Exception for missing archives in the file store.
/// </summary>
public class MissingArchiveException : Exception
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MissingArchiveException(Hash hash) : base($"Missing archive for {hash.ToHex()}") { }
    
    /// <summary>
    /// Constructor with an absolute path that specifies where the file would be extracted to
    /// </summary>
    public MissingArchiveException(Hash hash, AbsolutePath path) : base($"Missing archive for {hash.ToHex()} intended for output path {path}") { }
}


