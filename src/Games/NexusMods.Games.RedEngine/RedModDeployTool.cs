using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    public RedModDeployTool(ILogger<RedModDeployTool> logger)
    {
        _logger = logger;
    }
    public IEnumerable<GameDomain> Domains => new []{Cyberpunk2077.StaticDomain};

    private static readonly GamePath _redModPath = new(GameFolderType.Game, @"tools\redmod\bin\redmod.exe");
    private readonly ILogger<RedModDeployTool> _logger;

    public async Task Execute(Loadout loadout)
    {
        var exe = _redModPath.CombineChecked(loadout.Installation);
        
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        _logger.LogInformation("Running {Program}", exe);
        var result = await Cli.Wrap(exe.ToString())
            .WithArguments("deploy")
            .WithWorkingDirectory(loadout.Installation.Locations[GameFolderType.Game].ToString())
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .ExecuteAsync();
        _logger.LogInformation("Finished running {Program}", exe);
        
        if (result.ExitCode != 0)
        {
            _logger.LogError("RedMod Deploy failed with exit code {ExitCode}", result);
        }
    }

    public string Name => "RedMod Deploy";
}