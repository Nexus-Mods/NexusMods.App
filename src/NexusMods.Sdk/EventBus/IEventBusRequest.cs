using JetBrains.Annotations;

namespace NexusMods.Sdk.EventBus;

/// <summary>
/// Represents a request.
/// </summary>
[PublicAPI]
public interface IEventBusRequest<TResult> : IEventBusMessage
    where TResult : notnull;
