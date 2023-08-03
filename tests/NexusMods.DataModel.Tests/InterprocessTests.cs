using System.Buffers.Binary;
using System.Text.Json;
using DynamicData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Interprocess;
using NexusMods.Paths;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Tests;

public sealed class InterprocessTests : IDisposable
{
    private readonly TemporaryPath _sqliteFile;
    private readonly IMessageProducer<Message> _producer;
    private readonly IMessageConsumer<Message> _consumer;
    private readonly SqliteIPC _ipc;
    private readonly IInterprocessJobManager _jobManager;

    private class Message : IMessage
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

    public InterprocessTests(TemporaryFileManager fileManager, IServiceProvider serviceProvider, JsonSerializerOptions jsonSerializerOptions)
    {
        _sqliteFile = fileManager.CreateFile();
        _ipc = new SqliteIPC(serviceProvider.GetRequiredService<ILogger<SqliteIPC>>(), serviceProvider.GetRequiredService<IDataModelSettings>(), jsonSerializerOptions);
        _producer = new InterprocessProducer<Message>(_ipc);
        _consumer = new InterprocessConsumer<Message>(_ipc);
        _jobManager = _ipc;
    }

    [Fact]
    public async Task CanSendAndReceiveMessages()
    {
        var src = Enumerable.Range(0, 128).ToList();

        var dest = new List<int>();
        using var _ = _consumer.Messages.Subscribe(x =>
        {
            dest.Add(x.Value);
        });

        foreach (var i in src)
        {
            await _producer.Write(new Message { Value = i }, CancellationToken.None);
        }

        // Wait for the IPC reader loop to finish
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        dest.Should().BeEquivalentTo(src);
    }

    [JsonName(nameof(TestEntity))]
    private record TestEntity : Entity
    {
        public int Value { get; init; }

        public override EntityCategory Category => EntityCategory.InterprocessJob;
    }

    [Fact]
    public void TestJobTracking_SingleThread()
    {
        var magic = Random.Shared.Next();
        var updates = new List<(int Adds, int Updates, int Removes)>();

        _jobManager.Jobs
            .Filter(job => job.Payload is TestEntity testEntity && testEntity.Value == magic)
            .Subscribe(x =>
            {
                lock (updates) updates.Add((x.Adds, x.Updates, x.Removes));
            });

        using (var job = InterprocessJob.Create(_jobManager, new TestEntity { Value = magic }))
        {
            for (var x = 0.0; x < 1; x += 0.1)
            {
                job.Progress = new Percent(x);
            }
        }

        // The IPC should have picked up every addition, update and removal
        updates.Should().BeEquivalentTo(new[]
        {
            (1, 0, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 1, 0),
            (0, 0, 1)
        });
    }

    [Fact]
    public void TestJobTracking_MultipleThreads()
    {
        var magic = Random.Shared.Next();

        var totalAdds = 0;
        var totalUpdates = 0;
        var totalRemoves = 0;

        _jobManager.Jobs
            .Filter(job => job.Payload is TestEntity testEntity && testEntity.Value == magic)
            .Subscribe(x =>
            {
                Interlocked.Add(ref totalAdds, x.Adds);
                Interlocked.Add(ref totalUpdates, x.Updates);
                Interlocked.Add(ref totalRemoves, x.Removes);
            });

        var numThreads = Math.Min(10, Environment.ProcessorCount * 2);
        var threads = Enumerable.Range(0, numThreads)
            .Select(_ => new Thread(() =>
            {
                using var job = InterprocessJob.Create(_jobManager, new TestEntity { Value = magic });
                for (var x = 0.0; x < 1; x += 0.1)
                {
                    job.Progress = new Percent(x);
                }
            }))
            .ToArray();

        foreach(var thread in threads)
        {
            thread.Start();
        }

        foreach(var thread in threads)
        {
            thread.Join();
        }

        var combined = (totalAdds, totalUpdates, totalRemoves);
        combined.Should().Be((numThreads, numThreads * 10, numThreads));
    }

    [Fact]
    public void CanCleanup()
    {
        _ipc.CleanupOnce();
    }

    public void Dispose()
    {
        _ipc.Dispose();
        _sqliteFile.Dispose();
    }
}
