using NexusMods.Sdk.ProxyConsole;
using Table = NexusMods.Sdk.ProxyConsole.Table;

namespace NexusMods.CLI.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class LoggingRenderer : IRenderer
{
    public readonly List<IRenderable> Logs = new();

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

    /// <summary>
    /// Gets the last table's columns
    /// </summary>
    public IEnumerable<string> LastTableColumns => LastTable.Columns.OfType<Text>().Select(c => c.Template);

    /// <summary>
    /// Returns all the text that matches the given string found in the cells of the last table
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public IEnumerable<string> TableCellsWith(string text)
    {
        return LastTable.Rows.SelectMany(r => r.OfType<Text>()).Where(t => t.Template == text).Select(t => t.Template);
    }

    /// <summary>
    /// Returns all the text that matchestthe given string found in the cells of the given index the last table
    /// </summary>
    /// <param name="columnIdx"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public IEnumerable<string> TableCellsWith(int columnIdx, string text)
    {
        return LastTable.Rows
            .Where(row => row.Length > columnIdx)
            .Select(row => row[columnIdx])
            .OfType<Text>()
            .Where(t => t.Template == text)
            .Select(t => t.Template);
    }

    /// <summary>
    /// Returns all the text from all the cells of the last table in string format
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IEnumerable<string>> LastCellsAsStrings()
    {
        return from row in LastTable.Rows
            select (from cell in row
                   let text = cell is Text txt ? txt.Template : ""
                   select text);
    }

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
