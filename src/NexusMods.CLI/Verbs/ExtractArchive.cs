using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

public class ExtractArchive
{
    private readonly IRenderer _renderer;
    private readonly FileExtractor.FileExtractor _extractor;
    
    public ExtractArchive(Configurator configurator, FileExtractor.FileExtractor extractor)
    {
        _renderer = configurator.Renderer;
        _extractor = extractor;
    }
    
    public static VerbDefinition Definition = new("extract-archive",
        "Extracts an archive to a folder on-disk", new[]
        {
            new OptionDefinition<AbsolutePath>("i", "inputFile", "Input archive to extract"),
            new OptionDefinition<AbsolutePath>("o", "outputFolder", "Output location for files")
        });
    

    public async Task Run(AbsolutePath inputFile, AbsolutePath outputFolder, CancellationToken token)
    {
        await _extractor.ExtractAll(inputFile, outputFolder, token);
    }
}