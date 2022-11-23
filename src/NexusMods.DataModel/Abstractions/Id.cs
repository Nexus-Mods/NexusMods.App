using System.Buffers.Binary;

namespace NexusMods.DataModel;

public class Id : IEquatable<Id>
{
    public static readonly Id Empty = new Id(Guid.Empty);
    private Guid _guid = Guid.Empty;
    
    public Id(Guid guid)
    {
        _guid = guid;
    }

    public void Reset()
    {
        _guid = Guid.Empty;
    }

    public bool IsUnset => _guid == Guid.Empty;

    public void Write(Span<byte> buffer)
    {
        _guid.TryWriteBytes(buffer);
    }

    public static Id From(ReadOnlySpan<byte> data)
    {
        return new Id(new Guid(data));
    }

    public static Id New()
    {
        return new Id(Guid.NewGuid());
    }

    public void Set(Id id)
    {
        _guid = id._guid;
    }

    public long GetHashCode64()
    {
        const ulong kMul = 0x9E3779B97F4A7C15;
        Span<byte> span = stackalloc byte[16];
        Write(span);
        
        unchecked
        {
            var seed = BinaryPrimitives.ReadUInt64LittleEndian(span);
            var v = BinaryPrimitives.ReadUInt64LittleEndian(span[8..]);
        
            var a = (v ^ seed) * kMul;
            a ^= (a >> 47);
            var b = (seed ^ a) * kMul;
            b ^= (b >> 47);
            seed = b * kMul;
            return (long)seed;
        }
    }

    public bool Equals(Id other)
    {
        return _guid.Equals(other._guid);
    }

    public override bool Equals(object? obj)
    {
        return obj is Id other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _guid.GetHashCode();
    }
}