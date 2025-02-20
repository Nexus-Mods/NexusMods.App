using JetBrains.Annotations;

namespace NexusMods.Abstractions.EventBus;

/// <summary>
/// Handles requests by responding with a result.
/// </summary>
[PublicAPI]
public interface IEventBusRequestHandler<in TRequest, TResult>
    where TRequest : IEventBusRequest<TResult>
    where TResult : notnull
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    Task<TResult> Handle(TRequest request, CancellationToken cancellationToken);
}
