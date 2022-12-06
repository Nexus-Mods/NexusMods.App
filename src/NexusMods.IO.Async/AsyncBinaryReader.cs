using System.Buffers.Binary;
using System.Text;

namespace NexusMods.IO.Async;

public class AsyncBinaryReader
{
    private readonly Stream _s;
    private readonly Memory<byte> _buffer;
    private readonly Endian _endian;

    public AsyncBinaryReader(Stream s, Endian endian = Endian.Little)
    {
        _s = s;
        _buffer = new Memory<byte>(new byte[256]);
        _endian = endian;
    }

    public async ValueTask<byte> ReadByte()
    {
        await _s.ReadAllAsync(_buffer[..1]);
        return _buffer.Span[0];
    }

    public async ValueTask<ushort> ReadUInt16()
    {
        await _s.ReadAllAsync(_buffer[..2]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadUInt16BigEndian(_buffer[..2].Span) : 
            BinaryPrimitives.ReadUInt16LittleEndian(_buffer[..2].Span);
    }

    public async ValueTask<uint> ReadUInt32()
    {
        await _s.ReadAllAsync(_buffer[..4]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadUInt32BigEndian(_buffer[..4].Span) : 
            BinaryPrimitives.ReadUInt32LittleEndian(_buffer[..4].Span);
    }
    
    public async ValueTask<ulong> ReadUInt64()
    {
        await _s.ReadAllAsync(_buffer[..8]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadUInt64BigEndian(_buffer[..8].Span) : 
            BinaryPrimitives.ReadUInt64LittleEndian(_buffer[..8].Span);
    }
    
    public async ValueTask<short> ReadInt16()
    {
        await _s.ReadAllAsync(_buffer[..2]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadInt16BigEndian(_buffer[..2].Span) : 
            BinaryPrimitives.ReadInt16LittleEndian(_buffer[..2].Span);
    }

    public async ValueTask<int> ReadInt32()
    {
        await _s.ReadAllAsync(_buffer[..4]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadInt32BigEndian(_buffer[..4].Span) : 
            BinaryPrimitives.ReadInt32LittleEndian(_buffer[..4].Span);
    }
    
    public async ValueTask<long> ReadInt64()
    {
        await _s.ReadAllAsync(_buffer[..8]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadInt64BigEndian(_buffer[..8].Span) : 
            BinaryPrimitives.ReadInt64LittleEndian(_buffer[..8].Span);
    }
    
    public async ValueTask<float> ReadFloat()
    {
        await _s.ReadAllAsync(_buffer[..4]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadSingleBigEndian(_buffer[..4].Span) : 
            BinaryPrimitives.ReadSingleLittleEndian(_buffer[..4].Span);
    }
    
    public async ValueTask<double> ReadDouble()
    {
        await _s.ReadAllAsync(_buffer[..8]);
        return _endian == Endian.Big ? 
            BinaryPrimitives.ReadDoubleBigEndian(_buffer[..8].Span) : 
            BinaryPrimitives.ReadDoubleLittleEndian(_buffer[..8].Span);
    }

    public async ValueTask<byte[]> ReadBytes(int size)
    {
        var bytes = new byte[size];
        await _s.ReadAllAsync(bytes);
        return bytes;
    }

    public long Position
    {
        set => _s.Position = value;
        get => _s.Position;
    }

    public long Length => _s.Length;
    public Stream BaseStream => _s;

    public async Task<string> ReadFixedSizeString(ushort length, Encoding encoding)
    {
        var buf = new Memory<byte>(new byte[length]);
        await _s.ReadAllAsync(buf);
        return encoding.GetString(buf.Span);
    }
}