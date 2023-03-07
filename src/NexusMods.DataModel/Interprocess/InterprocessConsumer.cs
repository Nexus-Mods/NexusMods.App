using System.Reactive.Subjects;
using Cloudtoid.Interprocess;
using Microsoft.Extensions.Logging;

namespace NexusMods.DataModel.Interprocess;

public class InterprocessConsumer<T> : IMessageConsumer<T>, IDisposable where T : IMessage
{
    private readonly IQueueFactory _queueFactory;
    private readonly string _queueName;
    private readonly ISubscriber _queue;
    private CancellationTokenSource _tcs;
    private Subject<T> _messages = new();
    private readonly Task _task;
    private readonly ILogger<InterprocessConsumer<T>> _logger;

    public InterprocessConsumer(ILogger<InterprocessConsumer<T>> logger, IQueueFactory queueFactory)
    {
        _logger = logger;
        _queueFactory = queueFactory;
        _queueName = "NexusMods.DataModel.Interprocess." + typeof(T).Name;
        _queue = queueFactory.CreateSubscriber(new QueueOptions(_queueName, T.MaxSize * 16));

        _tcs = new CancellationTokenSource();
        _task = Task.Run(StartDequeueLoop);
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

    public IObservable<T> Messages => _messages;

    public void Dispose()
    {
        _tcs.Cancel();
        _queue.Dispose();
    }
}
