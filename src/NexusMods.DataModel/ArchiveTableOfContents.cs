using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel;

/// <summary>
/// Table of contents for an archive, this includes all the file paths in the archive, their hashes and sizes
/// </summary>
public record ArchiveTableOfContents : Entity
{
    /// <summary>
    /// The id of the archive this table of contents is for
    /// </summary>
    public required ArchiveId ArchiveId { get; init; }

    /// <summary>
    /// The entries in the archive
    /// </summary>
    public required ArchiveEntry[] Entries { get; init; }

    /// <inheritdoc />
    protected override IId Persist(IDataStore store)
    {
        var id = IdFor(ArchiveId);
        store.Put(id, this);
        return id;
    }

    /// <summary>
    /// Gets the id for the given archive id in the ArchiveTableOfContents category
    /// </summary>
    /// <param name="archiveId"></param>
    /// <returns></returns>
    public static IId IdFor(ArchiveId archiveId)
    {
        Span<byte> buffer = stackalloc byte[17];
        buffer[0] = (byte)EntityCategory.ArchiveTableOfContents;
        archiveId.Value.TryWriteBytes(buffer.SliceFast(1));
        return IId.FromSpan(EntityCategory.ArchiveTableOfContents, buffer);
    }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.ArchiveTableOfContents;
}


public record ArchiveEntry
{
    /// <summary>
    /// The hash of the file
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// The size of the file
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// The relative path of the file
    /// </summary>
    public required RelativePath Path { get; init; }
}
