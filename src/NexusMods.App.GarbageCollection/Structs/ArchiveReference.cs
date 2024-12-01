using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
namespace NexusMods.App.GarbageCollection.Structs;

/// <summary>
///     Stores a reference to a single archive.
/// </summary>
/// <typeparam name="TParsedHeaderState">
///     Contains pre-parsed header info for this type.
/// </typeparam>
public class ArchiveReference<TParsedHeaderState>
{
    /// <summary>
    ///     The native file path of the archive.
    /// </summary>
    public required AbsolutePath FilePath { get; init; }

    /// <summary>
    ///     All the hashes within the archive.
    /// </summary>
    public required Dictionary<Hash, HashEntry> Entries { get; init; }

    /// <summary>
    ///     The state of the header for the archive.
    /// </summary>
    public required TParsedHeaderState HeaderState { get; init; }
}
