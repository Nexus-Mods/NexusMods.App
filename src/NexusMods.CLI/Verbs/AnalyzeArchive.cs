using NexusMods.CLI.DataOutputs;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

public class AnalyzeArchive
{
    private readonly IRenderer _renderer;
    private readonly FileExtractor.FileExtractor _extractor;
    
    public AnalyzeArchive(Configurator configurator, FileExtractor.FileExtractor extractor)
    {
        _renderer = configurator.Renderer;
        _extractor = extractor;
    }
    
    public static VerbDefinition Definition = new("analyze-archive",
        "Analyzes the contents of an archive caches them, and outputs them", new[]
        {
            new OptionDefinition<AbsolutePath>("i", "inputFile", "File to Analyze")
        });



    public async Task Run(AbsolutePath inputFile, CancellationToken token)
    {
        var results = await _extractor.ForEachEntry(inputFile, async (path, factory) =>
        {
            await using var stream = await factory.GetStream();
            var hash = await stream.Hash(token);
            return new object[]
            {
                path, factory.Size, hash
            };
        }, token);

        await _renderer.Render(new Table(new[] { "Path", "Size", "Hash" }, results.Values.OrderBy(e => (RelativePath)e[0])));
    }
}