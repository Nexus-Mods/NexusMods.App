using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using Sewer56.BitStream;
using Sewer56.BitStream.ByteStreams;

namespace NexusMods.DataModel.Interprocess.Messages;

/// <summary>
/// Represents a change of a root from one id to another.
/// </summary>
public struct RootChange : IMessage
{
    /// <summary>
    /// The root type that changed.
    /// </summary>
    public required RootType Type { get; init; }

    /// <summary>
    /// The old Id of the root.
    /// </summary>
    public required IId From { get; init; }

    /// <summary>
    /// The new Id of the root.
    /// </summary>
    public required IId To { get; init; }

    /// <summary>
    /// We'll assume the max id size is 128 bytes.
    /// </summary>
    public static int MaxSize => 1 * 128 * 128;

    /// <inheritdoc />
    public unsafe int Write(Span<byte> buffer)
    {
        fixed (byte* ptr = buffer)
        {
            var stream = new PointerByteStream(ptr);
            var writer = new BitStream<PointerByteStream>(stream);
            writer.Write((byte)Type);

            writer.Write((byte)From.Category);
            writer.Write((byte)From.SpanSize);
            Span<byte> fromSpan = stackalloc byte[From.SpanSize];
            From.ToSpan(fromSpan);
            writer.Write(fromSpan);

            writer.Write((byte)To.Category);
            writer.Write((byte)To.SpanSize);
            Span<byte> toSpan = stackalloc byte[To.SpanSize];
            To.ToSpan(toSpan);
            writer.Write(toSpan);
        }

        // 1 byte for type, 2 bytes for each id (category + span size), 2 * span size for each id.
        return 5 + From.SpanSize + To.SpanSize;
    }

    /// <inheritdoc />
    public static unsafe IMessage Read(ReadOnlySpan<byte> buffer)
    {
        fixed (byte* ptr = buffer)
        {
            var stream = new PointerByteStream(ptr);
            var reader = new BitStream<PointerByteStream>(stream);
            var type = (RootType)reader.Read<byte>();

            var fromCategory = (EntityCategory)reader.Read<byte>();
            var fromSpanSize = reader.Read<byte>();
            Span<byte> fromSpan = stackalloc byte[fromSpanSize];
            reader.Read(fromSpan);
            var from = IId.FromSpan(fromCategory, fromSpan);

            var toCategory = (EntityCategory)reader.Read<byte>();
            var toSpanSize = reader.Read<byte>();
            Span<byte> toSpan = stackalloc byte[toSpanSize];
            reader.Read(toSpan);
            var to = IId.FromSpan(toCategory, toSpan);

            return new RootChange
            {
                Type = type,
                From = from,
                To = to
            };
        }
    }
}
