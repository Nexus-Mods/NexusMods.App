using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    private static readonly GamePath RedModPath = new(LocationId.Game, "tools/redmod/bin/redmod.exe");
    private static readonly GamePath RedModDeployFolder = new(LocationId.Game, "r6/cache/modded");

    private readonly ILogger<RedModDeployTool> _logger;
    private readonly IProcessFactory _processFactory;

    public RedModDeployTool(ILogger<RedModDeployTool> logger, IProcessFactory processFactory)
    {
        _logger = logger;
        _processFactory = processFactory;
    }

    public IEnumerable<GameDomain> Domains => new[] { Cyberpunk2077.Cyberpunk2077Game.StaticDomain };

    public async Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var exe = RedModPath.CombineChecked(loadout.InstallationInstance);
        var deployFolder = RedModDeployFolder.CombineChecked(loadout.InstallationInstance);
        
        // RedMod deploys to this folder and freaks out if it doesn't exist, but our synchronizer will
        // delete it if it's empty, so we need to recreate it here.
        if (!deployFolder.DirectoryExists())
            deployFolder.CreateDirectory();

        await _processFactory.ExecuteAsync(Cli.Wrap(exe.ToString())
            .WithArguments("deploy")
            .WithWorkingDirectory(exe.Parent.ToString()), cancellationToken: cancellationToken);
    }

    public string Name => "RedMod Deploy";
}
