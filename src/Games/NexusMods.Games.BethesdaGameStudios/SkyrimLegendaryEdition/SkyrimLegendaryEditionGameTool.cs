using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Common.OSInterop;
using NexusMods.DataModel.Abstractions.Games;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimLegendaryEditionGameTool : RunGameWithScriptExtender<SkyrimLegendaryEdition>
{
    // ReSharper disable once ContextualLoggerProblem
    public SkyrimLegendaryEditionGameTool(ILogger<RunGameTool<SkyrimLegendaryEdition>> logger, SkyrimLegendaryEdition game, IProcessFactory processFactory, IOSInterop osInterop)
        : base(logger, game, processFactory, osInterop) { }
    protected override GamePath ScriptLoaderPath => new(LocationId.Game, "skse_loader.exe");
}
