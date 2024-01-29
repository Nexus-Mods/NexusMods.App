using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.Games.BethesdaGameStudios.SkyrimLegendaryEdition;

public class SkyrimLegendaryEditionGameTool : RunGameWithScriptExtender<SkyrimLegendaryEdition>
{
    // ReSharper disable once ContextualLoggerProblem
    public SkyrimLegendaryEditionGameTool(ILogger<RunGameTool<SkyrimLegendaryEdition>> logger, SkyrimLegendaryEdition game, IProcessFactory processFactory, IOSInterop osInterop)
        : base(logger, game, processFactory, osInterop) { }
    protected override GamePath ScriptLoaderPath => new(LocationId.Game, "skse_loader.exe");
}
