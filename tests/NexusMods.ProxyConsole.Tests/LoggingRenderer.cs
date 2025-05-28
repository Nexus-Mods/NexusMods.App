using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.ProxyConsole.Tests;

public class LoggingRenderer : IRenderer
{
    public List<(string Method, object Message)> Messages { get; } = new();

    public ValueTask RenderAsync(IRenderable renderable)
    {
        Messages.Add(("RenderAsync", renderable));
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync()
    {
        Messages.Add(("ClearAsync", ""));
        return ValueTask.CompletedTask;
    }
}
