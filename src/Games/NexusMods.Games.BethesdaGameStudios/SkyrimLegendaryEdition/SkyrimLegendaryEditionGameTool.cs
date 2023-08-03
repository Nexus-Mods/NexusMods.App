using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimLegendaryEditionGameTool : RunGameWithScriptExtender<SkyrimLegendaryEdition>
{
    // ReSharper disable once ContextualLoggerProblem
    public SkyrimLegendaryEditionGameTool(ILogger<RunGameTool<SkyrimLegendaryEdition>> logger, SkyrimLegendaryEdition game, IProcessFactory processFactory) 
        : base(logger, game, processFactory) { }
    protected override GamePath ScriptLoaderPath => new(GameFolderType.Game, "skse_loader.exe");
}
