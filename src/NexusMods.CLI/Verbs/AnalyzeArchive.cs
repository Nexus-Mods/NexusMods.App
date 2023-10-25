using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Analyzes the contents of an archive caches them, and outputs them
/// </summary>
public class AnalyzeArchive : AVerb<AbsolutePath>, IRenderingVerb
{
    private readonly ILogger<AnalyzeArchive> _logger;
    private readonly IFileOriginRegistry _fileOriginRegistry;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="logger"></param>
    public AnalyzeArchive(ILogger<AnalyzeArchive> logger, IFileOriginRegistry fileOriginRegistry)
    {
        _logger = logger;
        _fileOriginRegistry = fileOriginRegistry;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("analyze-archive",
        "Analyzes the contents of an archive caches them, and outputs them",
        new OptionDefinition[]
        {
            new OptionDefinition<AbsolutePath>("i", "inputFile", "File to Analyze")
        });


    /// <inheritdoc />
    public async Task<int> Run(AbsolutePath inputFile, CancellationToken token)
    {
        try
        {
            var results = await Renderer.WithProgress(token, async () =>
            {
                var downloadId = await _fileOriginRegistry.RegisterDownload(inputFile, new FilePathMetadata
                {
                    OriginalName = inputFile.Name,
                    Quality = Quality.Low,
                    Name = inputFile.Name
                }, token);
                var metadata = await _fileOriginRegistry.Get(downloadId);
                return metadata.Contents.Select(kv =>
                {
                    return new object[]
                    {
                        kv.Path, kv.Size, kv.Hash
                    };
                });
            });

            await Renderer.Render(new Table(new[] { "Path", "Size", "Hash"},
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
