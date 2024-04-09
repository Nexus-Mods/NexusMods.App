using System.Buffers;
using System.Runtime.InteropServices;
using FlatSharp;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.DataModel.Serializers.DiskStateTreeSchema;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;
using Reloaded.Memory.Extensions;
using File = NexusMods.DataModel.Serializers.DiskStateTreeSchema.File;

namespace NexusMods.DataModel.Serializers;

/// <summary>
/// We want to store the disk state tree in the datom store, so we'll need to serialize it.
/// Long term we want to redo this, as we're just shoving potentially MB of data in a value, and
/// that's not really what MnemonicDB is designed for. It works for now and is fairly easy to fix
/// in the future, perhaps with MnemonicDB adding support for large values.
///
/// Another long-term option would be to store the tree *as* a tree in the datom store, that would
/// require more diffing logic, but would give us the ability to perform more complex audits.
/// </summary>
internal class DiskStateTreeSerializer : IValueSerializer<DiskStateTree>
{
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    public Type NativeType => typeof(DiskStateTree);
    public Symbol UniqueId => Symbol.Intern<DiskStateTreeSerializer>();
    
    public DiskStateTree Read(ReadOnlySpan<byte> buffer)
    {
        var decompressedSize = MemoryMarshal.Read<uint>(buffer);
        var compressedData = buffer.SliceFast(sizeof(uint));

        Files files;

        if (decompressedSize == buffer.Length - sizeof(uint))
        {
            files = Files.Serializer.Parse(compressedData.ToArray());
        }
        else
        {
            var decompressed = new byte[decompressedSize];
            unsafe
            {
                fixed (byte* src = compressedData)
                fixed (byte* dest = decompressed)
                {
                    Compression.Decompress(CompressionPreference.ZStandard, src, compressedData.Length,
                        dest, (int)decompressedSize
                    );
                }
            }
            files = Files.Serializer.Parse(decompressed);
        }
        
        var kvs = files.All
            .Select(f => new KeyValuePair<GamePath, DiskStateEntry>(new GamePath(LocationId.From(f.LocationId), f.Path),
                new DiskStateEntry
                {
                    Size = Size.From(f.Size),
                    Hash = Hash.From(f.Hash),
                    LastModified = DateTime.FromFileTimeUtc(f.LastWriteTime)
                }
            ));
        
        return DiskStateTree.Create(kvs);
    }

    public void Serialize<TWriter>(DiskStateTree tree, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        using var tmpWriter = new PooledMemoryBufferWriter();
        var toSerialize = new Files
        {
            All = tree.GetAllDescendentFiles()
                .Select(f => new File()
                    {
                        LocationId = f.Item.Id.Value,
                        Path = f.Item.ReconstructPath(),
                        Hash = f.Item.Value.Hash.Value,
                        Size = f.Item.Value.Size.Value,
                        LastWriteTime = f.Item.Value.LastModified.ToFileTimeUtc()
                    }
                ).ToList()
        };
        
        Files.Serializer.Write(tmpWriter, toSerialize);
        
        var sizeSpan = buffer.GetSpan(sizeof(uint));
        MemoryMarshal.Write(sizeSpan, (uint)tmpWriter.Length);
        buffer.Advance(sizeof(uint));

        var destSpan = buffer.GetSpan(Compression.MaxAllocForCompressSize(tmpWriter.Length));
        unsafe
        {
            fixed (byte* src = tmpWriter.GetWrittenSpan())
            fixed (byte* dest = destSpan)
            {
                var compressedSize = Compression.Compress(CompressionPreference.ZStandard, 9, 
                    src, tmpWriter.Length, 
                    dest, destSpan.Length, out var _);
                buffer.Advance(compressedSize);
            }    
        }
    }
}
