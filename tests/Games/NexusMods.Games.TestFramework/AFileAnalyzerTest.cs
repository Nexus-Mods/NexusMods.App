using System.Collections.Immutable;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
public class AFileAnalyzerTest<TGame, TFileAnalyzer> : AGameTest<TGame>
    where TGame : AGame
    where TFileAnalyzer : IFileAnalyzer
{
    protected readonly TFileAnalyzer FileAnalyzer;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AFileAnalyzerTest(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        FileAnalyzer = serviceProvider.FindImplementationInContainer<TFileAnalyzer, IFileAnalyzer>();
        _jsonSerializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
    }

    /// <summary>
    /// Uses <typeparamref name="TFileAnalyzer"/> to analyze the provided
    /// file and returns the analysis data.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    protected async Task<IFileAnalysisData[]> AnalyzeFile(AbsolutePath path)
    {
        await using var stream = path.Read();

        var asyncEnumerable = FileAnalyzer.AnalyzeAsync(new FileAnalyzerInfo
        {
            FileName = path.FileName,
            Stream = stream
        });

        var res = await asyncEnumerable.ToArrayAsync();

        var fileEntry = FileSystem.GetFileEntry(path);

        // Roundtrip the data through the serializer so we know it works
        // Forgetting to register some analysis types could cause the
        // Analyzer to look like it works, when it's really broken
        var analyzedFile = new AnalyzedFile
        {
            Size = fileEntry.Size,
            Hash = Hash.From(0xDEADBEEF),
            AnalyzersHash = Hash.From(0xCAFEFAB0),
            AnalysisData = res.ToImmutableList(),
            FileTypes = Array.Empty<FileType>()
        };

        var json = JsonSerializer.Serialize(analyzedFile, _jsonSerializerOptions);
        var deserialized = (AnalyzedFile)JsonSerializer.Deserialize<Entity>(json, _jsonSerializerOptions)!;

        return deserialized.AnalysisData.ToArray();
    }
}
