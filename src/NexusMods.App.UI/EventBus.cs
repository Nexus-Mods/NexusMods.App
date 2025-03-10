using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.EventBus;
using R3;

namespace NexusMods.App.UI;

public sealed class EventBus : IEventBus, IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private static IEventBus? _instance;
    public static IEventBus Instance
    {
        get => _instance ?? throw new InvalidOperationException("Event Bus hasn't been registered yet");
        private set
        {
            if (_instance is not null) throw new InvalidOperationException("Event Bus has already been registered");
            _instance = value;
        }
    }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly Subject<IEventBusMessage> _messages = new();

    public EventBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<EventBus>>();

        Instance = this;
    }

    [SuppressMessage("ReSharper", "HeapView.PossibleBoxingAllocation")]
    public void Send<T>(T message) where T : IEventBusMessage
    {
        _logger.LogDebug("Received message of type `{Type}`: `{Message}`", typeof(T), message.ToString());
        _messages.OnNext(message);
    }

    public Task<Optional<TResult>> SendAndReceive<TRequest, TResult>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IEventBusRequest<TResult>
        where TResult : notnull
    {
        Send(request);

        var requestHandler = _serviceProvider.GetService<IEventBusRequestHandler<TRequest, TResult>>();
        if (requestHandler is null)
        {
            _logger.LogError("Found no request handler for request of type `{RequestType}` with result type `{ResultType}`: `{StringRepresentation}`", typeof(TRequest), typeof(TResult), request.ToString());
            return Task.FromResult(Optional<TResult>.None);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(delay: DefaultTimeout);

        return Inner();

        async Task<Optional<TResult>> Inner()
        {
            try
            {
                return await requestHandler.Handle(request, cts.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception running request handler for request of type `{RequestType}` with result type `{ResultType}`: `{StringRepresentation}`", typeof(TRequest), typeof(TResult), request.ToString());
                return Optional<TResult>.None;
            }
        }
    }

    public Observable<T> ObserveMessages<T>() where T : IEventBusMessage
    {
        return _messages.OfType<IEventBusMessage, T>();
    }

    public Observable<IEventBusMessage> ObserveAllMessages()
    {
        return _messages;
    }

    private bool _isDisposed;
    public void Dispose()
    {
        if (_isDisposed) return;

        _messages.Dispose();
        _isDisposed = true;
    }
}
