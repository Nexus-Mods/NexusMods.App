using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Common.OSInterop;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimSpecialEditionGameTool : RunGameWithScriptExtender<SkyrimSpecialEdition>
{
    // ReSharper disable once ContextualLoggerProblem
    public SkyrimSpecialEditionGameTool(ILogger<RunGameTool<SkyrimSpecialEdition>> logger, SkyrimSpecialEdition game, IProcessFactory processFactory, IOSInterop osInterop)
        : base(logger, game, processFactory, osInterop) { }
    protected override GamePath ScriptLoaderPath => new(LocationId.Game, "skse64_loader.exe");
}
