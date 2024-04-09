using System.Buffers;
using System.Text;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Serializers;

internal class GameDomainSerializer : IValueSerializer<GameDomain>
{
    private static readonly Encoding _encoding = Encoding.UTF8;
    
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    public Type NativeType => typeof(GameDomain);
    public Symbol UniqueId => Symbol.Intern<GameDomainSerializer>();
    
    public GameDomain Read(ReadOnlySpan<byte> buffer)
    {
        return GameDomain.From(Encoding.UTF8.GetString(buffer));
    }

    public void Serialize<TWriter>(GameDomain value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var size = _encoding.GetByteCount(value.Value);
        var span = buffer.GetSpan(size);
        _encoding.GetBytes(value.Value, span);
        buffer.Advance(size);
    }
}
