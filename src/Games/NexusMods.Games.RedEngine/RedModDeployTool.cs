using CliWrap;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.Generic;
using NexusMods.Paths;
using static NexusMods.Games.RedEngine.Constants;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    private readonly GameToolRunner _toolRunner;

    public RedModDeployTool(GameToolRunner toolRunner) => _toolRunner = toolRunner;

    public IEnumerable<GameDomain> Domains => new[] { Cyberpunk2077.Cyberpunk2077Game.StaticDomain };

    public async Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        
        var exe = RedModPath.CombineChecked(loadout.InstallationInstance);
        var deployFolder = RedModDeployFolder.CombineChecked(loadout.InstallationInstance);
        
        // RedMod deploys to this folder and freaks out if it doesn't exist, but our synchronizer will
        // delete it if it's empty, so we need to recreate it here.
        if (!deployFolder.DirectoryExists())
            deployFolder.CreateDirectory();

        var fs = FileSystem.Shared;
        if (fs.OS.IsWindows)
        {
            var command = Cli.Wrap(exe.ToString())
                .WithArguments("deploy")
                .WithWorkingDirectory(exe.Parent.ToString());
            await _toolRunner.ExecuteAsync(loadout, command, true, cancellationToken);
        }
        else
        {
            var batchPath = fs.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/Cyberpunk2077/deploy_redmod.bat");
            await _toolRunner.ExecuteAsync(loadout, Cli.Wrap(batchPath.ToString()), true, cancellationToken);
        }
    }

    public string Name => "RedMod Deploy";
}
