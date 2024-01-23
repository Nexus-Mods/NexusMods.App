﻿using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Games.Tools;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

/// <summary>
/// A tool that allows running the game with a script extender.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class RunGameWithScriptExtender<T> : RunGameTool<T> where T : AGame {
    // ReSharper disable once ContextualLoggerProblem
    protected RunGameWithScriptExtender(ILogger<RunGameTool<T>> logger, T game, IProcessFactory processFactory, IOSInterop osInterop)
        : base(logger, game, processFactory, osInterop) { }

    protected abstract GamePath ScriptLoaderPath { get; }

    protected override async ValueTask<AbsolutePath> GetGamePath(Loadout loadout)
    {
        var flattened =
            await ((IStandardizedLoadoutSynchronizer)loadout.Installation.Game.Synchronizer)
            .LoadoutToFlattenedLoadout(loadout);
        return flattened.ContainsKey(ScriptLoaderPath) ?
            ScriptLoaderPath.CombineChecked(loadout.Installation) :
            await base.GetGamePath(loadout);
    }
}
