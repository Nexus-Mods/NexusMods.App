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

    public void Reset()
    {
        Logged.Clear();
    }
}