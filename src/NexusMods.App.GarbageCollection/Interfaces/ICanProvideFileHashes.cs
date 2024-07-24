using NexusMods.App.GarbageCollection.Structs;
namespace NexusMods.App.GarbageCollection.Interfaces;

/// <summary>
///     This is a marker interface for types which are capable of providing file hashes.
/// </summary>
public interface ICanProvideFileHashes
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    Span<Hash> GetFileHashes();
}
