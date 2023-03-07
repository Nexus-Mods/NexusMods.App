using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using Sewer56.BitStream;
using Sewer56.BitStream.ByteStreams;

namespace NexusMods.DataModel.Interprocess.Messages;

/// <summary>
/// Notice of a new id being inserted or updated.
/// </summary>
public struct IdPut : IMessage
{
    public IId Id { get; }
    public PutType Type { get; }

    public enum PutType : byte
    {
        Put,
        Delete
    }

    public IdPut(PutType type, IId id)
    {
        Type = type;
        Id = id;
    }
    public static int MaxSize { get; } = 1024 * 16;
    public int Write(Span<byte> buffer)
    {
        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                var stream = new PointerByteStream(ptr);
                var writer = new BitStream<PointerByteStream>(stream);

                writer.Write((byte)Type);
                writer.Write((byte)Id.Category);
                writer.Write(Id.SpanSize);
                Span<byte> fromSpan = stackalloc byte[Id.SpanSize];
                Id.ToSpan(fromSpan);
                writer.Write(fromSpan);
                return 2 + sizeof(int) + Id.SpanSize;
            }
        }
    }

    public static IMessage Read(ReadOnlySpan<byte> buffer)
    {
        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                var stream = new PointerByteStream(ptr);
                var reader = new BitStream<PointerByteStream>(stream);

                var type = reader.Read<byte>();
                var category = (EntityCategory)reader.Read<byte>();
                var spanSize = reader.Read<int>();
                Span<byte> fromSpan = stackalloc byte[spanSize];
                reader.Read(fromSpan);
                var id = IId.FromSpan(category, fromSpan);

                return new IdPut((PutType)type, id);
            }
        }
    }
}
