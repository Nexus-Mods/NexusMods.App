using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.FileStore.Nx.Models;

/// <summary>
/// Stores a NXArchive file entry as a blob.
/// </summary>
public class NxFileEntryAttribute(string ns, string name) : ScalarAttribute<FileEntry, Memory<byte>, BlobSerializer>(ns, name)
{
    /// <inheritdoc />
    protected override unsafe FileEntry FromLowLevel(Memory<byte> value, AttributeResolver resolver)
    {
        fixed (byte* ptr = value.Span)
        {
            var reader = new LittleEndianReader(ptr);
            FileEntry tmpEntry = default;
            tmpEntry.FromReaderV1(ref reader);
            return tmpEntry;
        }
    }

    /// <inheritdoc />
    protected override unsafe Memory<byte> ToLowLevel(FileEntry value)
    {
        var buffer = new byte[sizeof(FileEntry)];
        fixed (byte* ptr = buffer)
        {
            var interWriter = new LittleEndianWriter(ptr);
            value.WriteAsV1(ref interWriter);
        }
        return buffer;
    }


}
