using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    private static readonly GamePath RedModPath = new(LocationId.Game, "tools/redmod/bin/redmod.exe");
    private static readonly GamePath RedModDeployFolder = new(LocationId.Game, "r6/cache/modded");

    private readonly ILogger<RedModDeployTool> _logger;

    public RedModDeployTool(ILogger<RedModDeployTool> logger)
    {
        _logger = logger;
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

        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        _logger.LogInformation("Running {Program}", exe);
        var result = await Cli.Wrap(exe.ToString())
            .WithArguments("deploy")
            .WithWorkingDirectory(exe.Parent.ToString())
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .ExecuteAsync(cancellationToken);
        _logger.LogInformation("Finished running {Program}", exe);
        _logger.LogDebug("RedMod Deploy stdout: {StdOut}", stdOutBuffer.ToString());

        if (result.ExitCode != 0)
        {
            _logger.LogError("RedMod Deploy failed with exit code {ExitCode}", result.ToString());
        }
    }

    public string Name => "RedMod Deploy";
}
