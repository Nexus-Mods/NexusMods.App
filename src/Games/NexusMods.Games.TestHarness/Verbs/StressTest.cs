using Microsoft.Extensions.Logging;
using NexusMods.CLI;
using NexusMods.DataModel;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.Games.TestHarness.Verbs;

public class StressTest : AVerb<IGame>
{
    private readonly IRenderer _renderer;
    private readonly IServiceProvider _provider;

    public StressTest(ILogger<StressTest> logger, Configurator configurator, IServiceProvider provider, 
        LoadoutManager loadoutManager, FileContentsCache fileContentsCache)
    {
        _fileContentsCache = fileContentsCache;
        _renderer = configurator.Renderer;
        _provider = provider;
        _loadoutManager = loadoutManager;
        _logger = logger;
    }
    
    public static VerbDefinition Definition = 
        new VerbDefinition("stress-test", "Stress test the application by installing all recent mods for a given game", 
            new OptionDefinition[]
            {
                new OptionDefinition<IGame>("g", "game", "The game to install mods for")
            });

    private readonly LoadoutManager _loadoutManager;
    private readonly ILogger<StressTest> _logger;
    private readonly FileContentsCache _fileContentsCache;

    private HashSet<FileType> _skippedTypes = new()
    {
        FileType.PDF
    };

    protected override async Task<int> Run(IGame game, CancellationToken token)
    {
        var tests = RecentModsTest.Create(_provider, game);
        await _renderer.WithProgress(token, async () =>
        {
            await tests.Generate();
            return 0;
        });

        var failed = 0;
        
        _logger.LogInformation("Managing game");
        var loadOut = await _renderer.WithProgress(token, async () => 
            await _loadoutManager.ManageGame(game.Installations.First(), $"Stress Test + {DateTime.UtcNow}", token));
        
        foreach (var (fileInfo, path) in tests.GameRecords)
        {
            _logger.LogInformation("Analyzing {Name} {ModId} {FileId}", fileInfo.FileName, fileInfo.ModId, fileInfo.FileId);
            var analyzed = await _renderer.WithProgress(token, async () => await _fileContentsCache.AnalyzeFile(path, token));
            if (analyzed is not AnalyzedArchive aa )
            {
                _logger.LogInformation("Skipping {Name} {ModId} {FileId} because it is not an archive",
                    fileInfo.FileName, fileInfo.ModId, fileInfo.FileId);
                continue;
            }
            if (aa.Contents.All(f => f.Value.FileTypes.Any(t => _skippedTypes.Contains(t))))
            {
                _logger.LogInformation("Skipping {Name} {ModId} {FileId} because it does not contain any installable files",
                    fileInfo.FileName, fileInfo.ModId, fileInfo.FileId);
                continue;
            }
            
            

            _logger.LogInformation("Installing {Name} {ModId} {FileId}", fileInfo.FileName, fileInfo.ModId, fileInfo.FileId);
            
            
            var result = await _renderer.WithProgress(token, async () => 
                await loadOut.Install(path, fileInfo.FileName, token));
        }
        
        return failed;
    }
}