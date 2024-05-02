﻿using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

/// <summary>
/// A tool that allows running the game with a script extender.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class RunGameWithScriptExtender<T> : RunGameTool<T> where T : AGame {
    protected RunGameWithScriptExtender(IServiceProvider serviceProvider, T game)
        : base(serviceProvider, game) { }

    protected abstract GamePath ScriptLoaderPath { get; }

    protected override async ValueTask<AbsolutePath> GetGamePath(Loadout.Model loadout)
    {
        var flattened =
            await ((IStandardizedLoadoutSynchronizer)loadout.Installation.GetGame().Synchronizer)
            .LoadoutToFlattenedLoadout(loadout);
        return flattened.ContainsKey(ScriptLoaderPath) ?
            ScriptLoaderPath.CombineChecked(loadout.Installation) :
            await base.GetGamePath(loadout);
    }
}
