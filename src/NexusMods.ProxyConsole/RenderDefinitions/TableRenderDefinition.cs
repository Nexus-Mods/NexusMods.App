using NexusMods.Sdk.ProxyConsole;
using Rendering = Spectre.Console.Rendering;
using Console = Spectre.Console;
using Table = NexusMods.Sdk.ProxyConsole.Table;

namespace NexusMods.ProxyConsole.RenderDefinitions;

/// <summary>
/// A definition for rendering <see cref="Abstractions.Implementations.Table"/>s to the console using Spectre.Console.
/// </summary>
public class TableRenderDefinition() : ARenderableDefinition<Table>("2148B17A-D6EE-4ECF-9BB4-E70997DA2365")
{
    /// <inheritdoc />
    protected override async ValueTask<Rendering.IRenderable> ToSpectreAsync(Table table, Func<IRenderable, ValueTask<Rendering.IRenderable>> subConvert)
    {
        var t = new Console.Table();
        foreach (var column in table.Columns)
        {
            t.AddColumn(new Console.TableColumn(await subConvert(column)));
        }

        foreach (var row in table.Rows)
        {
            var convertedRow = new List<Rendering.IRenderable>();
            foreach (var cell in row)
            {
                convertedRow.Add(await subConvert(cell));
            }
            t.Rows.Add(convertedRow.ToArray());
        }
        return t;
    }
}
