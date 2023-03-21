using System.Buffers.Binary;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Interprocess;
using NexusMods.Paths;

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

    public InterprocessTests(TemporaryFileManager fileManager, IServiceProvider serviceProvider)
    {
        var file = fileManager.CreateFile();
        _ipc = new SqliteIPC(serviceProvider.GetRequiredService<ILogger<SqliteIPC>>(), file);
        _producer = new InterprocessProducer<Message>(_ipc);
        _consumer = new InterprocessConsumer<Message>(_ipc);
    }

    [Fact]
    public async Task CanSendAndReceiveMessages()
    {
        await Task.Delay(2000);
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

        while (dest.Count < src.Count)
        {
            await Task.Delay(100);
        }

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
