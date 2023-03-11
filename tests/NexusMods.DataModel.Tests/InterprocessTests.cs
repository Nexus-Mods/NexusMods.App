using System.Buffers.Binary;
using FluentAssertions;
using NexusMods.DataModel.Interprocess;

namespace NexusMods.DataModel.Tests;

public class InterprocessTests
{
    private readonly IMessageProducer<Message> _producer;
    private readonly IMessageConsumer<Message> _consumer;
    private readonly SqliteIPC _ipc;

    public class Message : IMessage
    {
        public int Value { get; init; }
        public static int MaxSize => 4;
        public int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32BigEndian(buffer, Value);
            return sizeof(int);
        }

        public static IMessage Read(ReadOnlySpan<byte> buffer)
        {
            var value = BinaryPrimitives.ReadInt32BigEndian(buffer);
            return new Message() { Value = value };
        }
    }

    public InterprocessTests(SqliteIPC ipc, IMessageProducer<Message> producer, IMessageConsumer<Message> consumer)
    {
        _ipc = ipc;
        _producer = producer;
        _consumer = consumer;
    }

    [Fact]
    public async Task CanSendAndReceiveMessages()
    {
        var src = Enumerable.Range(0, 128).ToList();
        var dest = new List<int>();
        using var _ = _consumer.Messages.Subscribe(x =>
        {
            lock (dest)
                dest.Add(x.Value);
        });

        await Task.Delay(1000);

        foreach (var i in src)
        {
            await _producer.Write(new Message { Value = i }, CancellationToken.None);
        }

        await Task.Delay(1000);

        // Dest is in a stack, so we'll reverse the src to make sure it all worked
        lock (dest)
            dest.Should().BeEquivalentTo(src, opt => opt.WithStrictOrdering());
    }

    [Fact]
    public async Task CanCleanup()
    {
        await _ipc.CleanupOnce(CancellationToken.None);
    }

}
