using NexusMods.Hashing.xxHash3;

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
}
