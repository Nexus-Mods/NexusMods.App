using DynamicData.Kernel;
using JetBrains.Annotations;
using R3;

namespace NexusMods.Abstractions.EventBus;

/// <summary>
/// Represents an event bus for sending messages and requests.
/// </summary>
[PublicAPI]
public interface IEventBus
{
    /// <summary>
    /// Sends a message.
    /// </summary>
    void Send<T>(T message) where T : IEventBusMessage;

    /// <summary>
    /// Sends a request as a message and returns task that completes when a handler responds with a result.
    /// </summary>
    Task<Optional<TResult>> SendAndReceive<TRequest, TResult>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IEventBusRequest<TResult>
        where TResult : notnull;

    /// <summary>
    /// Observes incoming messages of type <typeparamref name="T"/>.
    /// </summary>
    Observable<T> ObserveMessages<T>() where T : IEventBusMessage;

    /// <summary>
    /// Observes all incoming messages.
    /// </summary>
    Observable<IEventBusMessage> ObserveAllMessages();
}
