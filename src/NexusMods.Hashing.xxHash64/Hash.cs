using System.Buffers.Binary;
using Vogen;


namespace NexusMods.Hashing.xxHash64;

[ValueObject<ulong>]
public partial struct Hash
{
    public static readonly Hash Zero = From(0);
    
    public override string ToString()
    {
        return "0x"+ToHex();
    }
    
    public static implicit operator long(Hash a)
    {
        return BitConverter.ToInt64(BitConverter.GetBytes(a._value));
    }

    public static Hash FromLong(in long argHash)
    {
        return new Hash(BitConverter.ToUInt64(BitConverter.GetBytes(argHash)));
    }

    public static Hash FromULong(in ulong argHash)
    {
        return new Hash(argHash);
    }

    public static Hash FromHex(string xxHashAsHex)
    {
        Span<byte> bytes = stackalloc byte[8];
        xxHashAsHex.FromHex(bytes);
        return new Hash(BinaryPrimitives.ReadUInt64BigEndian(bytes));
    }

    public string ToHex()
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, _value);
        return ((ReadOnlySpan<byte>)buffer).ToHex();
    }
}