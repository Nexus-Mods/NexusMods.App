using System.Net;
using System.Runtime.InteropServices;
using FlatSharp;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Archives.Nx.Enums;
using NexusMods.DataModel.Serializers.DiskStateTreeSchema;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;
using File = NexusMods.DataModel.Serializers.DiskStateTreeSchema.File;

namespace NexusMods.DataModel.Attributes;

public class DiskStateAttribute(string ns, string name) : HashedBlobAttribute<DiskStateTree>(ns, name)
{
    protected override DiskStateTree FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag)
    {
        var files = Files.Serializer.Parse(value.ToArray());
        
        var res = GC.AllocateUninitializedArray<KeyValuePair<GamePath, DiskStateEntry>>(files.All.Count);
        
        for (var i = 0; i < files.All.Count; i++)
        {
            var f = files.All[i];
            res[i] = new KeyValuePair<GamePath, DiskStateEntry>(new GamePath(LocationId.From(f.LocationId), f.Path),
                new DiskStateEntry
                {
                    Size = Size.From(f.Size),
                    Hash = Hash.From(f.Hash),
                    LastModified = DateTime.FromFileTimeUtc(f.LastWriteTime),
                }
            );
        }
        
        return DiskStateTree.Create(res);
    }

    protected override void WriteValue<TWriter>(DiskStateTree value, TWriter writer)
    {
        var toSerialize = new Files
        {
            All = value.GetAllDescendentFiles()
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
        
        Files.Serializer.Write(writer, toSerialize);

    }
}
