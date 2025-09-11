using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine;

public class RunGameViaScriptExtenderTool<TGame> : RunGameTool<TGame>
    where TGame : AGame
{
    private RunGameViaScriptExtenderTool(IServiceProvider serviceProvider, TGame game, GamePath scriptExtenderPath) : base(serviceProvider, game)
    {
        ScriptExtenderPath = scriptExtenderPath;
    }

    public static IRunGameTool Create(IServiceProvider serviceProvider, GamePath scriptExtenderPath)
    {
        return new RunGameViaScriptExtenderTool<TGame>(serviceProvider, serviceProvider.GetRequiredService<TGame>(), scriptExtenderPath);
    }

    public GamePath ScriptExtenderPath { get; set; }

    protected override ValueTask<AbsolutePath> GetGamePath(Loadout.ReadOnly loadout)
    {
        var installationInstance = loadout.InstallationInstance;
        var game = installationInstance.GetGame();
        
        var extenderPath = installationInstance.LocationsRegister.GetResolvedPath(ScriptExtenderPath);
        if (extenderPath.FileExists)
            return ValueTask.FromResult(extenderPath);
        var primaryFile =  game.GetPrimaryFile(installationInstance.TargetInfo);
        return ValueTask.FromResult(installationInstance.LocationsRegister.GetResolvedPath(primaryFile)); 
    }
}
