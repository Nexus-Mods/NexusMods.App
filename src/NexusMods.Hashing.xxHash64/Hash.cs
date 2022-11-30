using System.Buffers.Binary;
using System.Buffers.Text;

namespace NexusMods.Hashing.xxHash64;

public struct Hash : IEquatable<Hash>, IComparable<Hash>
{
    public static readonly Hash Zero = new Hash(0);
    private readonly ulong _code;

    public Hash(ulong code = 0)
    {
        _code = code;
    }

    public override string ToString()
    {
        return "0x"+ToHex();
    }

    public bool Equals(Hash other)
    {
        return _code == other._code;
    }

    public int CompareTo(Hash other)
    {
        return _code.CompareTo(other._code);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Hash h)
            return h._code == _code;
        return false;
    }

    public override int GetHashCode()
    {
        return (int) (_code >> 32) ^ (int) _code;
    }

    public static bool operator ==(Hash a, Hash b)
    {
        return a._code == b._code;
    }

    public static bool operator !=(Hash a, Hash b)
    {
        return !(a == b);
    }

    public static explicit operator ulong(Hash a)
    {
        return a._code;
    }

    public static explicit operator long(Hash a)
    {
        return BitConverter.ToInt64(BitConverter.GetBytes(a._code));
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
        BinaryPrimitives.WriteUInt64BigEndian(buffer, _code);
        return ((ReadOnlySpan<byte>)buffer).ToHex();
    }
}