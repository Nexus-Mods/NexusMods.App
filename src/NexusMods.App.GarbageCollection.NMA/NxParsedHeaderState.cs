using System.Runtime.CompilerServices;
using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Hashing.xxHash64;
namespace NexusMods.App.GarbageCollection.NMA;

/// <summary>
///     The parsed header state for the Nx Archive.
/// </summary>
public class NxParsedHeaderState : ICanProvideFileHashes<FileEntryWrapper>
{
    /// <summary/>
    public ParsedHeader Header { get; }

    /// <summary/>
    public NxParsedHeaderState(ParsedHeader header) => Header = header;

    /// <inheritdoc />
    public Span<FileEntryWrapper> GetFileHashes() => Unsafe.As<FileEntryWrapper[]>(Header.Entries);
}

/// <summary>
///     This is a wrapper around the Nx <see cref="FileEntry"/>.
/// </summary>
public struct FileEntryWrapper : IHaveFileHash
{
    /// <summary>
    ///     The inner entry.
    /// </summary>
    private FileEntry Entry { get; }

    /// <inheritdoc />
    public Hash Hash => (Hash)Entry.Hash;
}
