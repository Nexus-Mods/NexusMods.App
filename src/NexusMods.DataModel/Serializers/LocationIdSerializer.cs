using System.Buffers;
using System.Text;
using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Serializers;

internal class LocationIdSerializer : IValueSerializer<LocationId>
{
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    public Type NativeType => typeof(LocationId);
    public Symbol UniqueId => Symbol.Intern<LocationIdSerializer>();
    
    public LocationId Read(ReadOnlySpan<byte> buffer)
    {
        return LocationId.From(Encoding.GetString(buffer));
    }

    public void Serialize<TWriter>(LocationId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var size = Encoding.GetByteCount(value.Value);
        var span = buffer.GetSpan(size);
        Encoding.GetBytes(value.Value, span);
        buffer.Advance(size);
    }
}
