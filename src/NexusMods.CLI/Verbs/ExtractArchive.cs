using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ExtractArchive : AVerb<AbsolutePath, AbsolutePath>
{
    private readonly FileExtractor.FileExtractor _extractor;
    
    public ExtractArchive(FileExtractor.FileExtractor extractor) => _extractor = extractor;

    public static VerbDefinition Definition => new("extract-archive",
        "Extracts an archive to a folder on-disk", new OptionDefinition[]
        {
            new OptionDefinition<AbsolutePath>("i", "inputFile", "Input archive to extract"),
            new OptionDefinition<AbsolutePath>("o", "outputFolder", "Output location for files")
        });
    

    public async Task<int> Run(AbsolutePath inputFile, AbsolutePath outputFolder, CancellationToken token)
    {
        await _extractor.ExtractAllAsync(inputFile, outputFolder, token);
        return 0;
    }
}