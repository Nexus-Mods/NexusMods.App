using NexusMods.DataModel.Interprocess;
using Sewer56.BitStream;
using Sewer56.BitStream.ByteStreams;

namespace NexusMods.DataModel.Abstractions;

public interface IDataStore
{
    public Id Put<T>(T value) where T : Entity;
    public void Put<T>(Id id, T value) where T : Entity;
    T? Get<T>(Id id, bool canCache = false) where T : Entity;

    bool PutRoot(RootType type, Id oldId, Id newId);
    Id? GetRoot(RootType type);
    byte[]? GetRaw(Id id);
    void PutRaw(Id key, ReadOnlySpan<byte> val);

    /// <summary>
    /// Delete the value for teh given id
    /// </summary>
    /// <param name="key"></param>
    void Delete(Id id);
    Task<long> PutRaw(IAsyncEnumerable<(Id Key, byte[] Value)> kvs, CancellationToken token = default);
    IEnumerable<T> GetByPrefix<T>(Id prefix) where T : Entity;
    IObservable<RootChange> RootChanges { get; }
    IObservable<Id> IdChanges { get; }
}

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
    public required Id From { get; init; }

    /// <summary>
    /// The new Id of the root.
    /// </summary>
    public required Id To { get; init; }

    /// <summary>
    /// We'll assume the max id size is 128 bytes.
    /// </summary>
    public static int MaxSize => 1 * 128 * 128;

    public int Write(Span<byte> buffer)
    {
        unsafe
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
        }
        // 1 byte for type, 2 bytes for each id (category + span size), 2 * span size for each id.
        return 5 + From.SpanSize + To.SpanSize;
    }

    public static IMessage Read(ReadOnlySpan<byte> buffer)
    {
        unsafe
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
                var from = Id.FromSpan(fromCategory, fromSpan);

                var toCategory = (EntityCategory)reader.Read<byte>();
                var toSpanSize = reader.Read<byte>();
                Span<byte> toSpan = stackalloc byte[toSpanSize];
                reader.Read(toSpan);
                var to = Id.FromSpan(toCategory, toSpan);

                return new RootChange
                {
                    Type = type,
                    From = from,
                    To = to
                };
            }
        }
    }
}

/// <summary>
/// Notice of a new id being inserted or updated.
/// </summary>
public struct IdPut : IMessage
{
    public Id Id { get; }
    public PutType Type { get; }

    public enum PutType : byte
    {
        Put,
        Delete
    }

    public IdPut(PutType type, Id id)
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
                var id = Id.FromSpan(category, fromSpan);

                return new IdPut((PutType)type, id);
            }
        }
    }
}
