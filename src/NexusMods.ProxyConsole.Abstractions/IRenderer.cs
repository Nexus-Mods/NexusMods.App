namespace NexusMods.ProxyConsole.Abstractions;

/// <summary>
/// An object that can render <see cref="IRenderable"/>s.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders the renderable to the console.
    /// </summary>
    /// <param name="renderable"></param>
    /// <returns></returns>
    public ValueTask RenderAsync(IRenderable renderable);

    /// <summary>
    /// Clears the console.
    /// </summary>
    /// <returns></returns>
    public ValueTask ClearAsync();
}
