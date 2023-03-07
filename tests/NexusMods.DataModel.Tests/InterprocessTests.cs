using System.Buffers.Binary;
using FluentAssertions;
using NexusMods.DataModel.Interprocess;

namespace NexusMods.DataModel.Tests;

public class InterprocessTests
{
    private readonly IMessageProducer<Message> _producer;
    private readonly IMessageConsumer<Message> _consumer;

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

    public InterprocessTests(IMessageProducer<Message> producer, IMessageConsumer<Message> consumer)
    {
        _producer = producer;
        _consumer = consumer;
    }

    [Fact]
    public async Task CanSendAndReceiveMessages()
    {
        var src = Enumerable.Range(0, 128).ToList();
        var dest = new List<int>();
        using var _ = _consumer.Messages.Subscribe(x => dest.Add(x.Value));

        foreach (var i in src)
        {
            await _producer.Write(new Message { Value = i }, CancellationToken.None);
        }

        await Task.Delay(500);
        dest.Should().BeEquivalentTo(src, opt => opt.WithStrictOrdering());
    }

}
