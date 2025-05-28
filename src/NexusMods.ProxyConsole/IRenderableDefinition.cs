using NexusMods.Sdk.ProxyConsole;
using Ansi = Spectre.Console.Rendering;

namespace NexusMods.ProxyConsole;

/// <summary>
/// An adapter for rendering <see cref="IRenderable"/>s to the console using Spectre.Console. Put here to decouple
/// the ProxyConsole.Abstractions from Spectre.Console.
/// </summary>
public interface IRenderableDefinition
{
    /// <summary>
    /// The type of <see cref="IRenderable"/> that this definition can render.
    /// </summary>
    public Type RenderableType { get; }

    /// <summary>
    /// The <see cref="Guid"/> that uniquely identifies this definition.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Converts the given <see cref="IRenderable"/> to a <see cref="IRenderToSpectre"/> that can be
    /// rendered to the console. Sub-renderables should be converted using the given subConvert function.
    /// </summary>
    /// <param name="renderable"></param>
    /// <param name="subConvert"></param>
    /// <returns></returns>
    public ValueTask<Ansi.IRenderable> ToSpectreAsync(IRenderable renderable, Func<IRenderable, ValueTask<Ansi.IRenderable>> subConvert);
}
