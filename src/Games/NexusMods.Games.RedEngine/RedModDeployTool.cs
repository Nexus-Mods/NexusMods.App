using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers.DTO;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    private static readonly GamePath RedModPath = new(LocationId.Game, "tools/redmod/bin/redmod.exe");

    private readonly ILogger<RedModDeployTool> _logger;

    public RedModDeployTool(ILogger<RedModDeployTool> logger)
    {
        _logger = logger;
    }

    public IEnumerable<GameDomain> Domains => new[] { Cyberpunk2077.StaticDomain };

    public async Task Execute(Loadout loadout, CancellationToken cancellationToken)
    {
        var exe = RedModPath.CombineChecked(loadout.Installation);

        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        _logger.LogInformation("Running {Program}", exe);
        var result = await Cli.Wrap(exe.ToString())
            .WithArguments("deploy")
            .WithWorkingDirectory(loadout.Installation.LocationsRegister[LocationId.Game].ToString())
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .ExecuteAsync();
        _logger.LogInformation("Finished running {Program}", exe);

        if (result.ExitCode != 0)
        {
            _logger.LogError("RedMod Deploy failed with exit code {ExitCode}", result.ToString());
        }
    }

    public string Name => "RedMod Deploy";
}
