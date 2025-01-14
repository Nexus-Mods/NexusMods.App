using System.Threading.Tasks;
using NexusMods.ProxyConsole.Abstractions.Implementations;

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

    /// <summary>
    /// Displays progress bars for tasks as long as the returned disposable is not disposed.
    /// </summary>
    public IDisposable WithProgress();

    /// <summary>
    /// Creates a progress bar, if not used inside a <see cref="WithProgress"/> block, it will not be displayed.
    /// </summary>
    public IProgress CreateProgress();
}
