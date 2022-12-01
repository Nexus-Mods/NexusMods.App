namespace NexusMods.CLI.Tests;

public class LoggingRenderer : IRenderer
{
    public List<object> Logged = new();
    public async Task Render<T>(T o)
    {
        Logged.Add(o);
    }

    public string Name => "logging";
    public void RenderBanner()
    {
        Logged.Add("Banner");
    }

    public Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true)
    {
        return f();
    }

    public void Reset()
    {
        Logged.Clear();
    }
}