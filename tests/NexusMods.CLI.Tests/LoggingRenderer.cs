namespace NexusMods.CLI.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class LoggingRenderer : IRenderer
{
    public static readonly AsyncLocal<List<Object>> Logs = new();

    public string Name => "logging";

    public Task Render<T>(T o)
    {
        Logs.Value!.Add(o!);
        return Task.CompletedTask;
    }

    public void RenderBanner()
    {
        Logs.Value!.Add("Banner");
    }

    public Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true)
    {
        return f();
    }

    public void Reset()
    {
        Logs.Value!.Clear();
    }
}
