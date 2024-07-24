namespace NexusMods.App.GarbageCollection.Interfaces;

/// <summary>
///     This is a marker interface for types which are capable of providing file hashes.
/// </summary>
public interface ICanProvideFileHashes<TIHaveFileHash>
{
    /// <summary>
    ///     Retrieves the hashes of all files.
    /// </summary>
    Span<TIHaveFileHash> GetFileHashes();
}
