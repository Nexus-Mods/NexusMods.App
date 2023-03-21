using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace NexusMods.App.UI.Routing;

/// <summary>
/// Global singleton class that handles navigation between pages. It gets messages
/// from various view models and presents them as an observable stream. This allows any
/// component in the system to broadcast a message and any other component to subscribe
/// to those messages and react to them.
/// </summary>
public class ReactiveMessageRouter : IRouter
{
    private readonly ILogger<ReactiveMessageRouter> _logger;

    public ReactiveMessageRouter(ILogger<ReactiveMessageRouter> logger)
    {
        _logger = logger;
    }
    public void NavigateTo(IRoutingMessage message)
    {
        _logger.LogDebug("Navigating to {Message}", message);
        _messages.OnNext(message);
    }

    private readonly Subject<IRoutingMessage> _messages = new();
    public IObservable<IRoutingMessage> Messages => _messages;
}
