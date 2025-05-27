using System;
using System.Threading.Tasks;

using Ansi = Spectre.Console.Rendering;
using IRenderable = NexusMods.Sdk.ProxyConsole.IRenderable;

namespace NexusMods.ProxyConsole.RenderDefinitions;

/// <summary>
/// A base class for <see cref="IRenderableDefinition"/>s that render a specific type of <see cref="IRenderable"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ARenderableDefinition<T>(string id) : IRenderableDefinition where T : IRenderable
{
    /// <summary>
    /// The type of <see cref="IRenderable"/> that this definition can render.
    /// </summary>
    public Type RenderableType => typeof(T);

    private readonly Guid _id = Guid.Parse(id);
    /// <inheritdoc />
    public Guid Id => _id;

    /// <inheritdoc />
    public ValueTask<Ansi.IRenderable> ToSpectreAsync(IRenderable renderable,
        Func<IRenderable, ValueTask<Ansi.IRenderable>> subConvert)
    {
        return ToSpectreAsync((T)renderable, subConvert);
    }

    /// <summary>
    /// Override this method to convert the given <see cref="IRenderable"/> to a <see cref="Ansi.IRenderable"/> that can be
    /// rendered to the console. Sub-renderables should be converted using the given subConvert function.
    /// </summary>
    /// <param name="renderable"></param>
    /// <param name="subConvert"></param>
    /// <returns></returns>
    protected abstract ValueTask<Ansi.IRenderable> ToSpectreAsync(T renderable,
        Func<IRenderable, ValueTask<Ansi.IRenderable>> subConvert);


}
