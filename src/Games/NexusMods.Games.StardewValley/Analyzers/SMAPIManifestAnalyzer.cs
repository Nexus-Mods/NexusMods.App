using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.StardewValley.Models;

// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Analyzers;

/// <summary>
/// <see cref="IFileAnalyzer"/> for mods that use the Stardew Modding API (SMAPI).
/// This looks for <c>manifest.json</c> files and returns <see cref="SMAPIManifest"/>.
/// </summary>
public class SMAPIManifestAnalyzer : IFileAnalyzer
{
    public FileAnalyzerId Id => FileAnalyzerId.New("f917e906-d28a-472d-b6e5-e7d2c61c60e4", 1);

    public IEnumerable<FileType> FileTypes => new[] { FileType.JSON };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly ILogger<SMAPIManifestAnalyzer> _logger;

    public SMAPIManifestAnalyzer(ILogger<SMAPIManifestAnalyzer> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken token = default)
    {
        if (!info.FileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            yield break;

        SMAPIManifest? result = null;
        try
        {
            result = await JsonSerializer.DeserializeAsync<SMAPIManifest>(info.Stream, JsonSerializerOptions, cancellationToken: token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while deserializing SMAPIManifest at {RelativePath}", info.RelativePath);
        }

        if (result is null) yield break;
        yield return result;
    }
}
