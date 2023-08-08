using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Analyzes the contents of an archive caches them, and outputs them
/// </summary>
public class AnalyzeArchive : AVerb<AbsolutePath>, IRenderingVerb
{
    private readonly IArchiveAnalyzer _archiveContentsCache;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="archiveContentsCache"></param>
    /// <param name="logger"></param>
    public AnalyzeArchive(IArchiveAnalyzer archiveContentsCache, ILogger<AnalyzeArchive> logger)
    {
        _logger = logger;
        _archiveContentsCache = archiveContentsCache;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("analyze-archive",
        "Analyzes the contents of an archive caches them, and outputs them",
        new OptionDefinition[]
        {
            new OptionDefinition<AbsolutePath>("i", "inputFile", "File to Analyze")
        });

    private readonly ILogger<AnalyzeArchive> _logger;

    /// <inheritdoc />
    public async Task<int> Run(AbsolutePath inputFile, CancellationToken token)
    {
        try
        {
            var results = await Renderer.WithProgress(token, async () =>
            {
                var file = await _archiveContentsCache.AnalyzeFileAsync(inputFile, token) as AnalyzedArchive;
                if (file == null) return Array.Empty<object[]>();
                return file.Contents.Select(kv =>
                {
                    return new object[]
                    {
                        kv.Key, kv.Value.Size, kv.Value.Hash,
                        string.Join(", ", kv.Value.FileTypes.Select(Enum.GetName))
                    };
                });
            });

            await Renderer.Render(new Table(new[] { "Path", "Size", "Hash", "Signatures" },
                results.OrderBy(e => (RelativePath)e[0])));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "While running");
            throw;
        }

        return 0;
    }
}
