using System.Runtime.CompilerServices;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;

namespace NexusMods.Abstractions.Cli;

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
    /// A table renderer for when you have a collection of tuples to render
    /// </summary>
    public static ValueTask Table<T>(this IRenderer renderer, IEnumerable<T> rows, params ReadOnlySpan<string> columnNames)
        where T : ITuple
    {
        var namesPrepared = GC.AllocateArray<IRenderable>(columnNames.Length);
        for (var i = 0; i < columnNames.Length; i++)
        {
            namesPrepared[i] = Renderable.Text(columnNames[i]);
        }

        static IRenderable[] PrepareRow(T row)
        {
            var rowPrepared = GC.AllocateArray<IRenderable>(row.Length);
            for (var i = 0; i < row.Length; i++)
            {
                rowPrepared[i] = Renderable.Text(row[i]!.ToString()!);
            }
            return rowPrepared;
        }

        return renderer.RenderAsync(new Table
            {
                Columns = namesPrepared,
                Rows = rows.Select(PrepareRow).ToArray(),
            }
        );
    }
    
    /// <summary>
    /// Renders the data in the given rows to a table
    /// </summary>
    public static ValueTask RenderTable<T>(this IEnumerable<T> rows, IRenderer renderer, params ReadOnlySpan<string> columnNames)
        where T : ITuple
    {
        return renderer.Table(rows, columnNames);
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
    /// Renders the text to the renderer with the given arguments and template
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="text"></param>
    public static async ValueTask<int> InputError(this IRenderer renderer, string template, params object[] args)
    {
        // Todo: implement custom conversion and formatting for the arguments
        await renderer.RenderAsync(Renderable.Text(template, args.Select(a => a.ToString()!).ToArray()));
        return -1;
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
        await renderer.Text(template, args);
        await renderer.Text("Error: {0}", ex);
    }
}
