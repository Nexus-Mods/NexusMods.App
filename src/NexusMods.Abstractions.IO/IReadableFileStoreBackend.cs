using NexusMods.Hashing.xxHash3;

namespace NexusMods.Abstractions.IO;

/// <summary>
/// A backend for the file store that allows getting read-only access to files
/// based on their hashes.
/// </summary>
public interface IReadableFileStoreBackend
{
    /// <summary>
    /// Returns true if there is an archive that has the specified file.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public ValueTask<bool> HaveFile(Hash hash);
    
    /// <summary>
    /// Gets a read-only seekable stream for the given file.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<Stream> GetFileStream(Hash hash, CancellationToken token = default);
}
