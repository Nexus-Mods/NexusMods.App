namespace NexusMods.App.UI.Routing;

public interface IRouter
{
    void NavigateTo(IRoutingMessage path);
    IObservable<IRoutingMessage> Messages { get; }
}
