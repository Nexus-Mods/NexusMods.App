using NexusMods.Abstractions.CLI;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;


namespace NexusMods.CLI.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class LoggingRenderer : IRenderer
{
    public static readonly List<IRenderable> Logs = new();

    public Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true)
    {
        return f();
    }

    /// <summary>
    /// Returns the number of items that were rendered
    /// </summary>
    public int Size => Logs.Count;

    /// <summary>
    /// Returns the last table that was rendered
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public Table LastTable => Logs.OfType<Table>().LastOrDefault() ??
                              throw new InvalidOperationException("No table was rendered");

    public T Last<T>() where T : IRenderable
    {
        return Logs.OfType<T>().LastOrDefault() ??
               throw new InvalidOperationException($"No {typeof(T).Name} was rendered");
    }

    public void Reset()
    {
        Logs.Clear();
    }

    public ValueTask RenderAsync(IRenderable renderable)
    {
        Logs.Add(renderable);
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync()
    {
        return ValueTask.CompletedTask;
    }
}
