using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.Games.BethesdaGameStudios.Fallout4;

public class Fallout4GameTool : RunGameWithScriptExtender<Fallout4>
{
    // ReSharper disable once ContextualLoggerProblem
    public Fallout4GameTool(ILogger<RunGameTool<Fallout4>> logger, Fallout4 game, IProcessFactory processFactory, IOSInterop osInterop)
        : base(logger, game, processFactory, osInterop) { }
    protected override GamePath ScriptLoaderPath => new(LocationId.Game, "f4se_loader.exe");
}
