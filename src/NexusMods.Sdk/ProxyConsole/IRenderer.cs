using JetBrains.Annotations;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// An object that can render implementations of <see cref="IRenderable"/>.
/// </summary>
[PublicAPI]
public interface IRenderer
{
    /// <summary>
    /// Renders the renderable to the console.
    /// </summary>
    ValueTask RenderAsync(IRenderable renderable);

    /// <summary>
    /// Clears the console.
    /// </summary>
    ValueTask ClearAsync();
}
