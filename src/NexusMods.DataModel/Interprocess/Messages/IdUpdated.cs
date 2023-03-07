using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using Sewer56.BitStream;
using Sewer56.BitStream.ByteStreams;

namespace NexusMods.DataModel.Interprocess.Messages;

/// <summary>
/// Notice of a new id being inserted or updated.
/// </summary>
public struct IdUpdated : IMessage
{
    // TODO: SkipLocalsInit here.

    /// <summary>
    /// The ID being inserted into the database.
    /// </summary>
    public IId Id { get; }

    /// <summary>
    /// Action related to the ID.
    /// </summary>
    public UpdateType Type { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="type">Type of update performed on this ID.</param>
    /// <param name="id"></param>
    public IdUpdated(UpdateType type, IId id)
    {
        Type = type;
        Id = id;
    }

    /// <inheritdoc />
    public static int MaxSize => 1024 * 16;

    /// <inheritdoc />
    public unsafe int Write(Span<byte> buffer)
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

    /// <inheritdoc />
    public static unsafe IMessage Read(ReadOnlySpan<byte> buffer)
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

            return new IdUpdated((UpdateType)type, id);
        }
    }

    /// <summary>
    /// Type of update performed on this ID.
    /// </summary>
    public enum UpdateType : byte
    {
        /// <summary>
        /// This ID is being inserted into the datastore.
        /// </summary>
        Put,

        /// <summary>
        /// This ID is being deleted from the datastore.
        /// </summary>
        Delete
    }
}
