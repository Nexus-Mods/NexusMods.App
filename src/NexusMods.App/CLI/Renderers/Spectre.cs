using NexusMods.CLI;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NexusMods.App.CLI.Renderers;

public class Spectre : IRenderer
{
    public string Name => "console";
    public void RenderBanner()
    {
        AnsiConsole.Write(new FigletText("NexusMods.App") {Color = NexusColor});
    }

    private Color NexusColor = new(0xda, 0x8e, 0x35);

    public async Task Render<T>(T o)
    {
        switch (o)
        {
            case NexusMods.CLI.DataOutputs.Table t:
                await RenderTable(t);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private async Task RenderTable(NexusMods.CLI.DataOutputs.Table table)
    {

        var ot = new Table();
        foreach (var column in table.Columns)
            ot.AddColumn(new TableColumn(new Text(column, new Style(foreground: NexusColor))));

        foreach (var row in table.Rows)
        {
            ot.AddRow(row.Select(r => r.ToString()).ToArray()!);
        }
        AnsiConsole.Write(ot);
    }
}