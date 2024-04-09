using System.Buffers;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Serializers;

internal class IIdSerializer : IValueSerializer<IId>
{
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    public Type NativeType => typeof(IId);
    public Symbol UniqueId => Symbol.Intern<IIdSerializer>();
    public IId Read(ReadOnlySpan<byte> buffer)
    {
        return IId.FromTaggedSpan(buffer);
    }

    public void Serialize<TWriter>(IId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(value.SpanSize + 1);
        value.ToTaggedSpan(span);
        buffer.Advance(value.SpanSize + 1);
    }
}
