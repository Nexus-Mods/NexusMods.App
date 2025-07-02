using NexusMods.Hashing.xxHash3;

namespace NexusMods.Abstractions.IO;

/// <summary>
/// An alternative read-only source of files could be a game store (e.g. Steam, GOG) or a some mod hostig service (e.g. Nexus Mods).
/// </summary>
public interface IReadOnlyFileStore
{
    /// <summary>
    /// Returns true if the file with the given hash exists in the store.
    /// </summary>
    ValueTask<bool> HaveFile(Hash hash);

    /// <summary>
    /// Get a filestream for the given file hash. Returns null if the file does not exist in the store.
    /// </summary>
    Task<Stream?> GetFileStream(Hash hash, CancellationToken token = default);
}
