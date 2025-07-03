using Microsoft.Extensions.DependencyInjection;
using NexusMods.ProxyConsole.Messages;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.ProxyConsole;

/// <summary>
/// A renderer that proxies render commands to a remote renderer.
/// </summary>
public class ProxiedRenderer : IRenderer
{
    private readonly Serializer _serializer;

    private ProxiedRenderer(Serializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Creates a new <see cref="ProxiedRenderer"/> instance from the given duplex capable stream.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="duplexStream"></param>
    /// <returns></returns>
    public static async Task<(string[] Arguments, IRenderer Renderer)> Create(IServiceProvider provider, Stream duplexStream)
    {
        var renderer = new ProxiedRenderer(new Serializer(duplexStream, provider.GetRequiredService<IEnumerable<IRenderableDefinition>>()));

        var arguments = await renderer._serializer.SendAndReceiveAsync<ProgramArgumentsResponse, ProgramArgumentsRequest>
            (new ProgramArgumentsRequest());

        return (arguments.Arguments, renderer);
    }

    /// <summary>
    /// Renders the given renderable.
    /// </summary>
    /// <param name="renderable"></param>
    public async ValueTask RenderAsync(IRenderable renderable)
    {
        await _serializer.SendAsync(new Render {Renderable = renderable});
    }

    /// <summary>
    /// Clears the console.
    /// </summary>
    public async ValueTask ClearAsync()
    {
        await _serializer.SendAsync(new Clear());
    }
}
