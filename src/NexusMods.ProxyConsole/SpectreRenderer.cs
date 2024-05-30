using Spectre.Console;
using Impl = NexusMods.ProxyConsole.Abstractions.Implementations;
using Render = Spectre.Console.Rendering;

namespace NexusMods.ProxyConsole;

/// <summary>
/// An adapter for rendering <see cref="Abstractions.IRenderable"/>s to the console using Spectre.Console.
/// </summary>
public class SpectreRenderer : Abstractions.IRenderer
{
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Wraps the given <see cref="IAnsiConsole"/> instance as a <see cref="Abstractions.IRenderer"/>.
    /// </summary>
    /// <param name="console"></param>
    public SpectreRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    /// <summary>
    /// Converts the given <see cref="Abstractions.IRenderable"/> to a <see cref="Render.IRenderable"/> that can be
    /// sent to Spectre.Console.
    /// </summary>
    /// <param name="renderable"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async ValueTask<Render.IRenderable> ToSpectreAsync(Abstractions.IRenderable renderable)
    {
        switch (renderable)
        {
            case Impl.Text text:
                if (text.Arguments.Length == 0)
                {
                    return new Text(text.Template);
                }
                else
                {
                    return new Text(string.Format(text.Template, text.Arguments));
                }
            case Impl.Table table:
                return await ToSpectreAsync(table);
            default:
                throw new NotImplementedException();
        }
    }

    private async ValueTask<Render.IRenderable> ToSpectreAsync(Impl.Table table)
    {
        var t = new Table();
        foreach (var column in table.Columns)
        {
            t.AddColumn(new TableColumn(await ToSpectreAsync(column)));
        }

        foreach (var row in table.Rows)
        {
            var convertedRow = new List<Render.IRenderable>();
            foreach (var cell in row)
            {
                convertedRow.Add(await ToSpectreAsync(cell));
            }
            t.AddRow(convertedRow.ToArray());
        }
        return t;
    }

    /// <inheritdoc />
    public async ValueTask RenderAsync(Abstractions.IRenderable renderable)
    {
        var spectre = await ToSpectreAsync(renderable);
        _console.Write(spectre);
    }

    /// <inheritdoc />
    public ValueTask ClearAsync()
    {
        _console.Clear();
        return ValueTask.CompletedTask;
    }
}
