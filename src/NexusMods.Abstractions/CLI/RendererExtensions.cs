using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using static NexusMods.Abstractions.CLI.Renderable;

namespace NexusMods.Abstractions.CLI;

/// <summary>
/// Extensions to the <see cref="IRenderer"/> interface to make rendering easier and to do common converssions
/// </summary>
public static class RendererExtensions
{
    /// <summary>
    /// Creates a new <see cref="Table"/> renderable from the given column names and rows.
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="columns"></param>
    /// <param name="rows"></param>
    public static async ValueTask Table(this IRenderer renderer, string[] columns, IEnumerable<object[]> rows)
    {
        await renderer.RenderAsync(new Table
        {
            Columns = columns.Select(s => (IRenderable)Renderable.Text(s)).ToArray(),
            Rows = rows.Select(r => r.Select(c => (IRenderable)Renderable.Text(c.ToString()!)).ToArray()).ToArray()
        });
    }

    /// <summary>
    /// Renders the given text to the renderer
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="text"></param>
    public static async ValueTask Text(this IRenderer renderer, string text)
    {
        await renderer.RenderAsync(Renderable.Text(text));
    }

    /// <summary>
    /// Renders the text to the renderer with the given arguments and template
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="text"></param>
    public static async ValueTask Text(this IRenderer renderer, string template, params object[] args)
    {
        // Todo: implement custom conversion and formatting for the arguments
        await renderer.RenderAsync(Renderable.Text(template, args.Select(a => a.ToString()!).ToArray()));
    }

    /// <summary>
    /// Runs the fn in a context that will render a progress bar while the user waits
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="fn"></param>
    public static async Task<T> WithProgress<T>(this IRenderer renderer, CancellationToken token, Func<Task<T>> fn)
    {
        // Need to implement this in the proxy interface
        return await fn();
    }

    /// <summary>
    /// Prints the given exception to the renderer
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="ex"></param>
    /// <param name="template"></param>
    /// <param name="args"></param>
    public static async Task Error(this IRenderer renderer, Exception ex, string template, params object[] args)
    {
        await renderer.Text(template + "\n\nTraceback: {ex} \n ", args);
    }
}
