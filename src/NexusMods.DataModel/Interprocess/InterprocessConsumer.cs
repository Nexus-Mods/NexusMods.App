using System.Reactive.Subjects;
using Cloudtoid.Interprocess;
using Microsoft.Extensions.Logging;

namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// Implementation of <see cref="IMessageConsumer{T}"/> that uses an interprocess queue.
/// </summary>
public sealed class InterprocessConsumer<T> : IMessageConsumer<T>, IDisposable where T : IMessage
{
    private readonly string _queueName;
    private readonly ISubscriber _queue;
    private CancellationTokenSource _tcs;
    private Subject<T> _messages = new();
    private readonly Task _task;
    private readonly ILogger<InterprocessConsumer<T>> _logger;

    /// <summary>
    /// Creates a consumer capable of receiving messages across process boundaries.
    /// </summary>
    /// <param name="logger">Logs received interprocess messages.</param>
    /// <param name="queueFactory">Factory used to create queues for receiving inter-process messages.</param>
    /// <remarks>
    ///    This method is usually called from DI container, not by user directly.
    /// </remarks>
    public InterprocessConsumer(ILogger<InterprocessConsumer<T>> logger, IQueueFactory queueFactory)
    {
        _logger = logger;
        _queueName = "NexusMods.DataModel.Interprocess." + typeof(T).Name;
        _queue = queueFactory.CreateSubscriber(new QueueOptions(_queueName, T.MaxSize * 16));

        _tcs = new CancellationTokenSource();
        _task = Task.Run(StartDequeueLoop);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _queue.Dispose();
        _tcs.Dispose();
        _messages.Dispose();

        if (_task.IsCompleted)
            _task.Dispose();
    }

    private async Task StartDequeueLoop()
    {
        var buffer = new Memory<byte>(new byte[T.MaxSize * 16]);
        while (!_tcs.Token.IsCancellationRequested)
        {
            if (_queue.TryDequeue(buffer, _tcs.Token, out var read))
            {
                _logger.LogTrace("Read {Size} byte message from queue {Queue}", read.Length, _queueName);
                _messages.OnNext((T)T.Read(read.Span));
                continue;
            }

            await Task.Delay(100, _tcs.Token);
        }
    }

    /// <inheritdoc />
    public IObservable<T> Messages => _messages;
}
