using System.Runtime.CompilerServices;
using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Hashing.xxHash64;
namespace NexusMods.App.GarbageCollection.Nx;

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
    
    // Note(Sewer): This cast may look suspicious, but it actually is safe.
    // This is mainly down to:
    // - FileEntryWrapper and FileEntry are value types
    // - Wrapping type doesn't enforce a different StructLayout.
    // Doing this sort of things with reference types are dangerous; since that 
    // would involve method tables and the likes. With raw value types, it's ok.
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
