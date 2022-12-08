using System.Collections.Concurrent;

namespace NexusMods.CLI.Tests;

public class LoggingRenderer : IRenderer
{
    public static AsyncLocal<List<Object>> Logs = new();
    public async Task Render<T>(T o)
    { 
        Logs.Value!.Add(o);
    }

    public string Name => "logging";
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