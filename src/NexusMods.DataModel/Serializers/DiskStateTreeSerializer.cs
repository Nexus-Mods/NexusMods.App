using System.Buffers;
using FlatSharp;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.DataModel.Serializers.DiskStateTreeSchema;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;
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
        var kvs = Files.Serializer.Parse(buffer.ToArray()).All
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
        Files.Serializer.Write(buffer, toSerialize);
    }
}
