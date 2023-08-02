using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

/// <summary>
/// A tool that allows running the game with a script extender.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class RunGameWithScriptExtender<T> : RunGameTool<T> where T : AGame {
    // ReSharper disable once ContextualLoggerProblem
    protected RunGameWithScriptExtender(ILogger<RunGameTool<T>> logger, T game, IProcessFactory processFactory)
        : base(logger, game, processFactory) { }
    
    protected abstract GamePath ScriptLoaderPath { get; }

    protected override AbsolutePath GetGamePath(Loadout loadout, ApplyPlan applyPlan)
    {
        return applyPlan.Flattened.ContainsKey(ScriptLoaderPath) ? 
            ScriptLoaderPath.CombineChecked(loadout.Installation) : 
            base.GetGamePath(loadout, applyPlan);
    }
}
