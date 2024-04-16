using NexusMods.Abstractions.DiskState;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.Attributes;

public class DiskStateAttribute(string ns, string name) : HashedBlobAttribute<DiskStateTree>(ns, name)
{
    protected override DiskStateTree FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag)
    {
        throw new NotImplementedException();
    }

    protected override void WriteValue<TWriter>(DiskStateTree value, TWriter writer)
    {
        throw new NotImplementedException();
    }
}
