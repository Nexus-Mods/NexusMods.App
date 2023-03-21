using System.Buffers.Binary;
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
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Tests;

public class InterprocessTests
{
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

    public InterprocessTests(TemporaryFileManager fileManager, IServiceProvider serviceProvider)
    {
        var file = fileManager.CreateFile();
        _ipc = new SqliteIPC(serviceProvider.GetRequiredService<ILogger<SqliteIPC>>(), file);
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

    [Fact]
    public async Task CanCreateJobs()
    {
        var updates = new HashSet<(int Adds, int Removes)>();
        var jobId = new Id64(EntityCategory.TestData, 42);
        _jobManager.Jobs
            .Filter(job => job.JobType == JobType.ManageGame)
            .Filter(job => job.PayloadAsId.Equals(jobId))
            .Subscribe(x =>
            {
                lock (updates)
                    updates.Add((x.Adds, x.Removes));
            });

        {
            using var job = new InterprocessJob(JobType.ManageGame, _jobManager,
                jobId, "Test");
            for (var x = 0.0; x < 1; x += 0.1)
            {
                await Task.Delay(10);
                job.Progress = new Percent(x);
            }
        }
        await Task.Delay(1000);
        // One update, one removal
        updates.Should().BeEquivalentTo(new[]
        {
            (1, 0),
            (0, 1)
        });
    }

    [Fact]
    public async Task CanCleanup()
    {
        await _ipc.CleanupOnce(CancellationToken.None);
    }

}
