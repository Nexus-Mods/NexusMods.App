using System.CommandLine;
using System.CommandLine.IO;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.SingleProcess;

internal class ConsoleToRendererAdapter(IRenderer renderer) : IConsole
{
    public IStandardStreamWriter Out { get; } = new StreamWriterAdapter(renderer);
    public bool IsOutputRedirected => true;
    public IStandardStreamWriter Error { get; } = new StreamWriterAdapter(renderer);
    public bool IsErrorRedirected => true;
    public bool IsInputRedirected => true;
}

internal class StreamWriterAdapter(IRenderer renderer) : IStandardStreamWriter
{
    public void Write(string? value)
    {
        renderer.RenderAsync(new Text {Template = value ?? string.Empty})
            .AsTask()
            .Wait(CancellationToken.None);
    }
}
