using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.FileAnalyzers;

public class RedModInfoAnalyzer : IFileAnalyzer
{
    public IEnumerable<FileType> FileTypes => new[] { FileType.JSON };
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(Stream stream, CancellationToken ct = default)
    {
        var info = await JsonSerializer.DeserializeAsync<InfoJson>(stream, cancellationToken: ct);
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
    public required string Name { get; init; }
}