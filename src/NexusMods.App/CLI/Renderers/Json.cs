using System.Text.Json;
using NexusMods.CLI;
using NexusMods.CLI.DataOutputs;

namespace NexusMods.App.CLI.Renderers;

/// <summary>
/// IRenderer that swallows progress messages and renders the final result as JSON
/// </summary>
public class Json : IRenderer
{
    private readonly JsonSerializerOptions _options;

    public Json(JsonSerializerOptions options)
    {
        _options = options;
    }
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
                JsonSerializer.Serialize(writer, cell, _options);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        await writer.FlushAsync();
    }

    public string Name => "json";
    public void RenderBanner() { }

    public Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true)
    {
        return f();
    }
}