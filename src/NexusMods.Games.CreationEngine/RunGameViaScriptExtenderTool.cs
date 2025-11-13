using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Games.CreationEngine;

public class RunGameViaScriptExtenderTool<TGame> : RunGameTool<TGame> where TGame : IGame
{
    private readonly GamePath _scriptExtenderPath;
    
    private RunGameViaScriptExtenderTool(IServiceProvider serviceProvider, TGame game, GamePath scriptExtenderPath) : base(serviceProvider, game)
    {
        _scriptExtenderPath = scriptExtenderPath;
    }

    public static IRunGameTool Create(IServiceProvider serviceProvider, GamePath scriptExtenderPath)
    {
        return new RunGameViaScriptExtenderTool<TGame>(serviceProvider, serviceProvider.GetRequiredService<TGame>(), scriptExtenderPath);
    }

    protected override ValueTask<AbsolutePath> GetGamePath(Loadout.ReadOnly loadout)
    {
        var installationInstance = loadout.InstallationInstance;
        var extenderPath = installationInstance.Locations.ToAbsolutePath(_scriptExtenderPath);
        if (extenderPath.FileExists) return ValueTask.FromResult(extenderPath);
        return base.GetGamePath(loadout);
    }
}
