using System.Buffers.Binary;
using System.Text.Json;
using DynamicData;
using DynamicData.PLinq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Interprocess;
using NexusMods.Paths;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Tests;

public class InterprocessTests : IDisposable
{
    private readonly TemporaryPath _sqliteFile;
    private readonly IMessageProducer<Message> _producer;
    private readonly IMessageConsumer<Message> _consumer;
    private readonly SqliteIPC _ipc;
    private readonly IInterprocessJobManager _jobManager;

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

    [JsonName(nameof(TestEntity))]
    private record TestEntity : Entity
    {
        public int Value { get; init; }

        public override EntityCategory Category => EntityCategory.InterprocessJob;
    }

    [Fact]
    public void TestJobTracking_SingleThread_HighTimeout()
    {
        var magic = Random.Shared.Next();
        var updates = new List<(int Adds, int Updates, int Removes)>();

        // SqliteIPC pools every 100ms
        var timeout = TimeSpan.FromMilliseconds(300);

        _jobManager.Jobs
            .Filter(job => job.Payload is TestEntity testEntity && testEntity.Value == magic)
            .Subscribe(x =>
            {
                lock (updates) updates.Add((x.Adds, x.Updates, x.Removes));
            });

        using (var job = InterprocessJob.Create(_jobManager, new TestEntity { Value = magic }))
        {
            // waits for the IPC to pickup the newly created job
            Thread.Sleep(timeout);

            for (var x = 0.0; x < 1; x += 0.1)
            {
                job.Progress = new Percent(x);

                // waits for the IPC to pickup the update
                Thread.Sleep(timeout);
            }
        }

        // waits for the IPC to pickup the removed job
        Thread.Sleep(timeout);

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
    public void TestJobTracking_MultipleThreads_HighTimeout()
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

        // SqliteIPC pools every 100ms
        var timeout = TimeSpan.FromMilliseconds(300);

        var numThreads = Math.Min(10, Environment.ProcessorCount * 2);
        var threads = Enumerable.Range(0, numThreads)
            .Select(_ => new Thread(() =>
            {
                using var job = InterprocessJob.Create(_jobManager, new TestEntity { Value = magic });
                // waits for the IPC to pickup the newly created job
                Thread.Sleep(timeout);

                for (var x = 0.0; x < 1; x += 0.1)
                {
                    job.Progress = new Percent(x);

                    // waits for the IPC to pickup the update
                    Thread.Sleep(timeout);
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

        // waits for the IPC to pickup the removed jobs
        Thread.Sleep(timeout);

        // The IPC should have picked up every addition, update and removal
        totalAdds.Should().Be(numThreads);
        totalRemoves.Should().Be(totalAdds);
        totalUpdates.Should().Be(totalAdds * 10);
    }

    [Fact]
    public async Task CanCleanup()
    {
        await _ipc.CleanupOnce(CancellationToken.None);
    }

    public void Dispose()
    {
        _ipc.Dispose();
        _sqliteFile.Dispose();
    }
}
