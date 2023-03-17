using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.Games.RedEngine.FileAnalyzers;

public class RedModInfoAnalyzer : IFileAnalyzer
{
    public IEnumerable<FileType> FileTypes => new[] { FileType.JSON };
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(Stream stream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        InfoJson? info;
        try
        {
            info = await JsonSerializer.DeserializeAsync<InfoJson>(stream, cancellationToken: ct);
        }
        catch (JsonException)
        {
            yield break;
        }
        if (info != null)
            yield return new RedModInfo { Name = info.Name };
    }
}

internal class InfoJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

[JsonName("NexusMods.Games.RedEngine.FileAnalyzers.RedModInfo")]
public record RedModInfo : IFileAnalysisData
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required string Name { get; init; }
}
