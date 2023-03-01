using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class RunStandaloneTool: ITool
{
    private readonly ILogger _logger;
    private readonly LauncherManagerFactory _launcherManagerFactory;

    public string Name => $"Run {MountAndBlade2Bannerlord.DisplayName}";
    public IEnumerable<GameDomain> Domains => new[] { MountAndBlade2Bannerlord.StaticDomain };

    public RunStandaloneTool(ILogger<RunStandaloneTool> logger, LauncherManagerFactory launcherManagerFactory)
    {
        _logger = logger;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public async Task Execute(Loadout loadout)
    {
        if (loadout.Installation.Game is not MountAndBlade2Bannerlord mountAndBladeBannerlord) return;

        var hasXbox = false; // TODO:
        var hasBLSE = loadout.HasModuleId("Bannerlord.BLSE");
        if (hasXbox && !hasBLSE) return; // Not supported.

        var program = hasBLSE
            ? mountAndBladeBannerlord.BLSEStandaloneFile.CombineChecked(loadout.Installation.Locations[GameFolderType.Game])
            : mountAndBladeBannerlord.PrimaryStandaloneFile.CombineChecked(loadout.Installation.Locations[GameFolderType.Game]);
        _logger.LogInformation("Running {Program}", program);

        var launcherManager = _launcherManagerFactory.Get(loadout.Installation);
        var psi = new ProcessStartInfo(program.ToString())
        {
            Arguments = launcherManager.ExecutableParameters
        };
        var process = Process.Start(psi);
        await process.WaitForExitAsync();
        _logger.LogInformation("Finished running {Program}", program);
    }
}
