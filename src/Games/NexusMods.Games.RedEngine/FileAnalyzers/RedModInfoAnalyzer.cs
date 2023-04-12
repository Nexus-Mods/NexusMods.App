using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.FileAnalyzers;

public class RedModInfoAnalyzer : IFileAnalyzer
{
    public FileAnalyzerId Id { get; } = FileAnalyzerId.New("8bf03d10-c48e-4929-906f-852b6afecd5e", 1);
    
    public IEnumerable<FileType> FileTypes => new[] { FileType.JSON };
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // TODO: We can probably check by fileName here before blindly deserializing - Sewer.
        InfoJson? jsonInfo;
        try
        {
            jsonInfo = await JsonSerializer.DeserializeAsync<InfoJson>(info.Stream, cancellationToken: ct);
        }
        catch (JsonException)
        {
            yield break;
        }
        if (jsonInfo != null)
            yield return new RedModInfo { Name = jsonInfo.Name };
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
