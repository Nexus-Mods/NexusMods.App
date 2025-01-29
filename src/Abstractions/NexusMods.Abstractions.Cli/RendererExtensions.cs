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
    /// Renders the given text to the renderer and follows it up with a newline
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="text"></param>
    public static async ValueTask TextLine(this IRenderer renderer, string text)
    {
        await renderer.RenderAsync(Renderable.Text(text + "\n"));
    }
    
    /// <summary>
    /// Starts a progress bar box in the renderer that will be stopped when the returned disposable is disposed
    /// </summary>
    public static async Task<IAsyncDisposable> WithProgress(this IRenderer renderer)
    {
        await renderer.RenderAsync(new StartProgress());

        return new DisposableProgress(renderer);
    }

    private class DisposableProgress(IRenderer renderer) : IAsyncDisposable
    { 
        public async ValueTask DisposeAsync()
        {
            await renderer.RenderAsync(new StopProgress());
        }
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
    /// Renders the text to the renderer with the given arguments and template, followed by a newline
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="text"></param>
    public static async ValueTask TextLine(this IRenderer renderer, string template, params object[] args)
    {
        // Todo: implement custom conversion and formatting for the arguments
        await renderer.RenderAsync(Renderable.Text(template + "\n", args.Select(a => a.ToString()!).ToArray()));
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
    
    /// <summary>
    /// Prints an error message to the renderer, and returns an error code
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="ex"></param>
    /// <param name="template"></param>
    /// <param name="args"></param>
    public static async Task Error(this IRenderer renderer, string template, params object[] args)
    {
        await renderer.Text(template, args);
    }

    /// <summary>
    /// Creates a new progress task with the given text, the task will be deleted when the returned disposable is disposed
    /// </summary>
    public static async ValueTask<ProgressTask> StartProgressTask(this IRenderer renderer, string text, double? maxValue = null)
    {
        var taskId = Guid.NewGuid();
        await renderer.RenderAsync(new CreateProgressTask() { TaskId = taskId, Text = text });
        return new ProgressTask(renderer, taskId, maxValue);
    }

    /// <summary>
    /// Wraps the enumeration in a progress task that will update the progress bar as the items are enumerated
    /// </summary>
    public static async IAsyncEnumerable<T> WithProgress<T>(this T[] items, IRenderer renderer, string text)
    {
        var increment = 1.0 / items.Length;
        await using var task = await renderer.StartProgressTask(text);
        
        foreach (var item in items)
        {
            await task.IncrementProgress(increment);
            yield return item;
        }
    }
    
    /// <summary>
    /// Wraps the enumeration in a progress task that will update the progress bar as the items are enumerated
    /// </summary>
    public static async IAsyncEnumerable<T> WithProgress<T>(this IEnumerable<T> items, IRenderer renderer, string text)
    {
        var array = items.ToArray();
        var increment = 1.0 / array.Length;
        await using var task = await renderer.StartProgressTask(text);
        
        foreach (var item in array)
        {
            await task.IncrementProgress(increment);
            yield return item;
        }
    }
}
