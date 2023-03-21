namespace NexusMods.App.UI.Routing;

/// <summary>
/// Interface for a backend component that routes messages between UI components.
/// </summary>
public interface IRouter
{
    void NavigateTo(IRoutingMessage path);
    IObservable<IRoutingMessage> Messages { get; }
}
