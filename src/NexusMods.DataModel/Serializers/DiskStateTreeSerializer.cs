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
