using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.CrossPlatform.Process;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;

public class SkyrimSpecialEditionGameTool : RunGameWithScriptExtender<SkyrimSpecialEdition>
{
    // ReSharper disable once ContextualLoggerProblem
    public SkyrimSpecialEditionGameTool(ILogger<RunGameTool<SkyrimSpecialEdition>> logger, SkyrimSpecialEdition game, IProcessFactory processFactory, IOSInterop osInterop)
        : base(logger, game, processFactory, osInterop) { }
    protected override GamePath ScriptLoaderPath => new(LocationId.Game, "skse64_loader.exe");
}
