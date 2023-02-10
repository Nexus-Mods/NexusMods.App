using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

public class ExtractArchive : AVerb<AbsolutePath, AbsolutePath>
{
    private readonly IRenderer _renderer;
    private readonly FileExtractor.FileExtractor _extractor;
    
    public ExtractArchive(Configurator configurator, FileExtractor.FileExtractor extractor)
    {
        _renderer = configurator.Renderer;
        _extractor = extractor;
    }
    
    public static readonly VerbDefinition Definition = new("extract-archive",
        "Extracts an archive to a folder on-disk", new[]
        {
            new OptionDefinition<AbsolutePath>("i", "inputFile", "Input archive to extract"),
            new OptionDefinition<AbsolutePath>("o", "outputFolder", "Output location for files")
        });
    

    protected override async Task<int> Run(AbsolutePath inputFile, AbsolutePath outputFolder, CancellationToken token)
    {
        await _extractor.ExtractAllAsync(inputFile, outputFolder, token);
        return 0;
    }
}