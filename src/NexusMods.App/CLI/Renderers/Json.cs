using System.Text.Json;
using NexusMods.CLI;
using NexusMods.CLI.DataOutputs;

namespace NexusMods.App.CLI.Renderers;

public class Json : IRenderer
{
    public async Task Render<T>(T o)
    {
        if (o is Table t)
        {
            await RenderTable(t);
            return;
        }

        throw new NotImplementedException();
    }

    private async Task RenderTable(Table table)
    {
        await using var stdOut = Console.OpenStandardOutput();
        var writer = new Utf8JsonWriter(stdOut);
        writer.WriteStartArray();
        foreach (var row in table.Rows)
        {
            writer.WriteStartObject();
            foreach (var (column, cell) in table.Columns.Zip(row))
            {
                writer.WritePropertyName(column);
                JsonSerializer.Serialize(writer, cell.ToString(), JsonSerializerOptions.Default);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        await writer.FlushAsync();
    }

    public string Name => "json";
    public void RenderBanner()
    {
    }
}