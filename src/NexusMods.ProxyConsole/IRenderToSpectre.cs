using NexusMods.Sdk.ProxyConsole;
using Spectre.Console;

namespace NexusMods.ProxyConsole;

/// <summary>
/// An adapter for rendering <see cref="IRenderable"/>s to the console using Spectre.Console. Put here to decouple
/// ProxyConsole.Abstractions from Spectre.Console.
/// </summary>
public interface IRenderToSpectre
{
    /// <summary>
    /// Render the given <see cref="IRenderable"/> to the given <see cref="IAnsiConsole"/>.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="renderable"></param>
    /// <returns></returns>
    public ValueTask RenderAsync(IAnsiConsole console, IRenderable renderable);
}
