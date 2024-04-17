using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.ArchiveContents;

/// <summary>
/// Stores a NXArchive file entry as a blob.
/// </summary>
public class NxFileEntryAttribute(string ns, string name) : BlobAttribute<FileEntry>(ns, name)
{
    /// <inheritdoc />
    protected override unsafe FileEntry FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag)
    {
        fixed (byte* ptr = value)
        {
            var reader = new LittleEndianReader(ptr);
            FileEntry tmpEntry = default;
            tmpEntry.FromReaderV1(ref reader);
            return tmpEntry;
        }
    }

    /// <inheritdoc />
    protected override unsafe void WriteValue<TWriter>(FileEntry value, TWriter writer)
    {
        var buffer = writer.GetSpan(sizeof(FileEntry));
        fixed (byte* ptr = buffer)
        {
            var interWriter = new LittleEndianWriter(ptr);
            value.WriteAsV1(ref interWriter);
            writer.Advance(sizeof(FileEntry));
        }
    }
}
